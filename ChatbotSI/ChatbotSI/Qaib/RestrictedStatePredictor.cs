using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatbotSI
{
    [Serializable()]
    public class RestrictedStatePredictor
    {
        public int trainingDepth;

        public int inputSymbolSize;
        public int outputSymbolSize;
        public int stateSize;

        public byte[] predictionTable;
        public float[] predictionAccuracy;
        public int tableStride;

        public int[][] predictionAccumulationTable;

        public byte[] stateOnlyPredictionTable;
        public int[][] stateOnlyAccumulationTable;
        public float[] stateOnlyPredictionAccuracy;

        public byte[] inputOnlyPredictionTable;
        public int[][] inputOnlyAccumulationTable;
        public float[] inputOnlyPredictionAccuracy;


        public ushort[] stateTransitionTable;
        public int stateStride;

        public ushort state;

        public int predictionCount;
        public int errorCount;
        public int[] stateMetrics;

        public float stateEntropy
        {
            get
            {
                float information = 0;
                double prob;
                for (int i = 0; i < stateMetrics.Length; i++)
                {
                    prob = stateMetrics[i] / (double)predictionCount;
                    if (prob > 0)
                        information -= (float)(prob * Math.Log(prob, stateSize));
                }
                return information;
            }
        }

        public float accuracy
        {
            get { return 1f - errorCount / (float)predictionCount; }
        }

        private RestrictedStatePredictor()
        {

        }
        public RestrictedStatePredictor(int inputSymbolSize,int outputSymbolSize, int stateSize)
        {
            this.inputSymbolSize = inputSymbolSize;
            this.outputSymbolSize = outputSymbolSize;
            this.stateSize = stateSize;

            //Create table with input(inputSymbolSize , stateSize) each with 2 entries ( output , newState )
            predictionTable = new byte[inputSymbolSize * stateSize];
            //Stride, bytes per symbol (states + (prediction , newState))
            tableStride = stateSize; 
            stateMetrics = new int[stateSize];
            //i0 , s0: p00 , s00
            //i0 , s1: p01 , s01
            //...

            //Create state transition table based on what was seen and what should have been the prediction ( input, correct)
            stateTransitionTable = new ushort[inputSymbolSize * stateSize];
            stateStride = stateSize;


            //Help structures
            predictionAccuracy = new float[inputSymbolSize * stateSize];

            predictionAccumulationTable = new int[inputSymbolSize * stateSize][];
            for (int i = 0; i < predictionAccumulationTable.Length; i++)
                predictionAccumulationTable[i] = new int[outputSymbolSize];

            stateOnlyPredictionTable = new byte[stateSize];
            stateOnlyPredictionAccuracy = new float[stateSize];
            stateOnlyAccumulationTable = new int[stateSize][];
            for (int i = 0; i < stateSize; i++)
                stateOnlyAccumulationTable[i] = new int[outputSymbolSize];

            inputOnlyPredictionTable = new byte[inputSymbolSize];
            inputOnlyPredictionAccuracy = new float[inputSymbolSize];
            inputOnlyAccumulationTable = new int[inputSymbolSize][];
            for (int i = 0; i < inputSymbolSize; i++)
                inputOnlyAccumulationTable[i] = new int[outputSymbolSize];
        }
        
        public void reset()
        {
            state = 0;
            predictionCount = 0;
            errorCount = 0;
            for (int i = 0; i < stateMetrics.Length; i++)
                stateMetrics[i] = 0;
        }

        //increments optimal prediction table
        public void bake(byte input, byte expected)
        {
            stateMetrics[state]++;

            int index = (input * tableStride + state);
            predictionAccumulationTable[index][expected]++; //Increment frequency

            state = stateTransitionTable[index]; //Transition
        }

        //Finalize baking to compute error and produce a predictor
        public void finalizeBake()
        {
            int wIndex;
            int bestCount;
            byte bestSymbol;
            int predCount;
            // Create prediction table based on input values
            for (int i = 0; i < stateSize; i++)
            {

                for (int j = 0; j < inputSymbolSize; j++)
                {
                    wIndex = j * tableStride + i;

                    bestCount = predictionAccumulationTable[wIndex][0];
                    stateOnlyAccumulationTable[i][0] += predictionAccumulationTable[wIndex][0];
                    inputOnlyAccumulationTable[j][0] += predictionAccumulationTable[wIndex][0];
                    predictionAccumulationTable[wIndex][0] = 0;
                    bestSymbol = 0;
                    predCount = bestCount;
                    for (byte k = 1; k < outputSymbolSize; k++)
                    {
                        stateOnlyAccumulationTable[i][k] += predictionAccumulationTable[wIndex][k];
                        inputOnlyAccumulationTable[j][k] += predictionAccumulationTable[wIndex][k];

                        predCount += predictionAccumulationTable[wIndex][k];

                        if (predictionAccumulationTable[wIndex][k] > bestCount)
                        {
                            bestCount = predictionAccumulationTable[wIndex][k];
                            bestSymbol = k;
                        }

                        predictionAccumulationTable[wIndex][k] = 0;
                    }

                    predictionTable[wIndex] = (byte)bestSymbol;

                    if (predCount != 0)
                        predictionAccuracy[wIndex] = bestCount / (float)predCount;
                    else
                        predictionAccuracy[wIndex] = 0;

                    errorCount += predCount - bestCount;
                    predictionCount += predCount;
                }
            }

            //State estimator
            for (int i = 0; i < stateSize; i++)
            {
                bestCount = stateOnlyAccumulationTable[i][0];
                bestSymbol = 0;
                predCount = bestCount;
                stateOnlyAccumulationTable[i][0] = 0;
                for (byte k = 1; k < outputSymbolSize; k++)
                {
                    predCount += stateOnlyAccumulationTable[i][k];

                    if (stateOnlyAccumulationTable[i][k] > bestCount)
                    {
                        bestCount = stateOnlyAccumulationTable[i][k];
                        bestSymbol = k;
                    }

                    stateOnlyAccumulationTable[i][k] = 0;
                }

                stateOnlyPredictionTable[i] = bestSymbol;
                if (predCount != 0)
                    stateOnlyPredictionAccuracy[i] = bestCount / (float)predCount;
                else
                    stateOnlyPredictionAccuracy[i] = 0;
            }

            //Input only estimator
            for (int i = 0; i < inputSymbolSize; i++)
            {
                bestCount = inputOnlyAccumulationTable[i][0];
                bestSymbol = 0;
                predCount = bestCount;
                inputOnlyAccumulationTable[i][0] = 0;
                for (byte k = 1; k < outputSymbolSize; k++)
                {
                    predCount += inputOnlyAccumulationTable[i][k];

                    if (inputOnlyAccumulationTable[i][k] > bestCount)
                    {
                        bestCount = inputOnlyAccumulationTable[i][k];
                        bestSymbol = k;
                    }

                    inputOnlyAccumulationTable[i][k] = 0;
                }

                inputOnlyPredictionTable[i] = bestSymbol;
                if (predCount != 0)
                    inputOnlyPredictionAccuracy[i] = bestCount / (float)predCount;
                else
                    inputOnlyPredictionAccuracy[i] = 0;
            }

            //Rebake main table for sparse values elimination using InputOnly and StateOnly estimators
            for (int i = 0; i < inputSymbolSize; i++)
            {
                for (int j = 0; j < stateSize; j++)
                {
                    wIndex = i * tableStride + j;

                    //If predictionTable is invalid on element
                    if (predictionAccuracy[wIndex] == 0)
                    {
                        //Check mosst accurate state/input based prediction
                        if (inputOnlyPredictionAccuracy[i] > stateOnlyPredictionAccuracy[j])
                        {
                            //Use input only
                            predictionTable[wIndex] = inputOnlyPredictionTable[i];
                            predictionAccuracy[wIndex] = inputOnlyPredictionAccuracy[i];
                        }
                        else
                        {
                            //Use state only
                            predictionTable[wIndex] = stateOnlyPredictionTable[j];
                            predictionAccuracy[wIndex] = stateOnlyPredictionAccuracy[j];
                        }
                    }
                }
            }
        }

        //Process and predict one single input
        public byte process(byte input)
        {
            int index = input * tableStride + state;
            state = stateTransitionTable[index];
            return predictionTable[index];
        }

        public string getDescription()
        {
            string stats = "";

            stats += "Training Depth: " + trainingDepth + "\n";
            stats += "Symbol size: " + inputSymbolSize + "\n";
            stats += "State size: " + stateSize + "\n";
            stats += "Prediction Count: " + predictionCount + "\n";
            stats += "Accuracy: " + accuracy + "\n";
            stats += "State entropy: " + stateEntropy + "\n";

            return stats;
        }
        public string getStats()
        {
            string stats = "";
            stats += " :: depth. " + (trainingDepth + "").PadRight(20).Substring(0, 6);
            stats += " :: acc.: " + accuracy;
            stats += " :: pc.: " + predictionCount;
            stats += " :: s.entropy: " + ("" + stateEntropy).PadRight(10).Substring(0, 4);
            return stats;
        }

        public class ProcessResult
        {
            public byte[] predictions;
            public byte[] hits;
            public ushort[] states;
        }
        public class CorpusResult
        {
            public SymbolCorpus predictions;
            public SymbolCorpus hits;
            public SymbolCorpus states;
        }

        public static void copyInto(RestrictedStatePredictor original, RestrictedStatePredictor destination)
        {
            original.predictionTable.CopyTo(destination.predictionTable, 0);
            destination.tableStride = original.tableStride;
            destination.errorCount = original.errorCount;
            destination.predictionCount = original.predictionCount;
            destination.state = original.state;
            destination.stateSize = original.stateSize;
            destination.inputSymbolSize = original.inputSymbolSize;
            destination.trainingDepth = original.trainingDepth;
            original.stateMetrics.CopyTo(destination.stateMetrics, 0);
            original.stateTransitionTable.CopyTo(destination.stateTransitionTable, 0);
            destination.stateStride = original.stateStride;
        }
    }
}
