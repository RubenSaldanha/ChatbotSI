using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatbotSI
{
    //TODO serializable
    public class StatePredictor
    {
        public int lastLeapCount;
        public double lastLeapIntensity
        {
            get { return lastLeapCount / (double)table.Length; }
        }

        public int trainingDepth;

        public int symbolSize;
        public int stateSize;

        byte[] table;
        short tableStride;

        byte state;

        public int predictionCount;
        public int errorCount;
        int[] stateMetrics;

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

        public StatePredictor(int symbolSize, int stateSize)
        {
            this.symbolSize = symbolSize;
            this.stateSize = stateSize;

            //Create table with input(symbolSize , stateSize) each with 2 entries ( prediction , newState )
            table = new byte[symbolSize * stateSize * 2];
            //Stride, bytes per symbol (states + (prediction , newState))
            tableStride = (short)(stateSize * 2); // stateSize times 2 output bytes
            stateMetrics = new int[stateSize];
            //i0 , s0: p00 , s00
            //i0 , s1: p01 , s01
            //...

            lastLeapCount = (int)(table.Length * 0.99f);
        }

        public void reset()
        {
            state = 0;
            predictionCount = 0;
            errorCount = 0;
            for (int i = 0; i < stateMetrics.Length; i++)
                stateMetrics[i] = 0;
        }

        //Make a single prediction and update state
        public byte predict(byte input)
        {
            //Increment one prediction made with current state
            stateMetrics[state]++;

            //Object index
            int index = (input * tableStride + state * 2);

            //Change state
            state = table[index + 1];

            //Return prediction
            return table[index + 0];
        }
        //Predicts a batch and returns predictions
        public byte[] predict(byte[] input)
        {
            byte[] predictions = new byte[input.Length];

            //initialize error counter
            int errors = 0;

            //Make first prediction and store it, it doesn't have input (default 0)
            state = 0;
            predictions[0] = predict(0);

            //Check if it was mistaken
            if (predictions[0] != input[0])
                errors++;

            //Make the rest of the predictions
            for (int i = 1; i < input.Length; i++)
            {
                predictions[i] = predict(input[i - 1]);

                if (predictions[i] != input[i])
                    errors++;
            }

            errorCount += errors;
            predictionCount += predictions.Length;
            return predictions;
        }

        //Predicts a batch and returns states used
        public byte[] predictStates(byte[] input)
        {
            byte[] states = new byte[input.Length];
            int errors = 0;

            byte prediction;
            //Make first prediction, it doesn't have input (default 0)
            state = 0;
            states[0] = 0;
            prediction = predict(0);
            if (prediction != input[0])
                errorCount++;

            //Make the rest of the predictions
            for (int i = 1; i < input.Length; i++)
            {
                states[i] = state;
                prediction = predict(input[i - 1]);

                if (prediction != input[i])
                    errors++;
            }

            errorCount += errors;
            predictionCount += input.Length;

            return states;
        }
        //Predicts a corpus and returns states used in corpus
        public SymbolCorpus predictStates(SymbolCorpus input)
        {
            //Create same dimension state Corpus
            SymbolCorpus stateCorpus = new SymbolCorpus();
            stateCorpus.dialogues = new SymbolDialogue[input.dialogues.Length];

            //Process
            SymbolDialogue inputDialogue;
            SymbolSentence inputSentence;
            for(int i=0;i<input.dialogues.Length;i++)
            {
                inputDialogue = input.dialogues[i];

                //Create same dimension state dialogue
                stateCorpus.dialogues[i] = new SymbolDialogue();
                stateCorpus.dialogues[i].sentences = new SymbolSentence[input.dialogues[i].sentences.Length];

                for (int j=0;j<inputDialogue.sentences.Length;j++)
                {
                    inputSentence = inputDialogue.sentences[j];

                    stateCorpus.dialogues[i].sentences[j] = new SymbolSentence();

                    //predict corpus, reset state per sentence
                    state = 0;
                    stateCorpus.dialogues[i].sentences[j].symbols = predictStates(inputSentence.symbols);
                }
            }

            return stateCorpus;
        }

        //Predicts only to update error
        public void testPredict(byte[] input)
        {
            int errors = 0;

            byte prediction;
            //Make first prediction, it doesn't have input (default 0 which is the void '_')
            state = 0;
            prediction = predict(0);
            if (prediction != input[0])
                errorCount++;

            //Make the rest of the predictions
            for (int i = 1; i < input.Length; i++)
            {
                prediction = predict(input[i - 1]);

                if (prediction != input[i])
                    errors++;
            }

            errorCount += errors;
            predictionCount += input.Length;
        }
        //Predicts corpus only to update error
        public void testPredict(SymbolCorpus input)
        {
            //Process
            SymbolDialogue inputDialogue;
            SymbolSentence inputSentence;
            for (int i = 0; i < input.dialogues.Length; i++)
            {
                inputDialogue = input.dialogues[i];

                for (int j = 0; j < inputDialogue.sentences.Length; j++)
                {
                    inputSentence = inputDialogue.sentences[j];

                    //predict corpus, reset state per sentence
                    state = 0;
                    testPredict(inputSentence.symbols);
                }
            }
        }

        //predicts and returns true on sucessful predictions
        public byte[] predictHits(byte[] input)
        {
            byte[] hits = new byte[input.Length];

            byte prediction;
            //Make first prediction, it doesn't have input (default 0 which is the void '_')
            state = 0;
            prediction = predict(0);
            hits[0] = (byte)((prediction == input[0]) ? 1 : 0);

            //Make the rest of the predictions
            for (int i = 1; i < input.Length; i++)
            {
                prediction = predict(input[i - 1]);

                hits[i] = (byte)((prediction == input[i]) ? 1 : 0);
            }

            return hits;
        }
        //Predicts and returns true on sucessful predictions
        public SymbolCorpus predictHits(SymbolCorpus input)
        {
            //Create same dimension hit Corpus
            SymbolCorpus hitCorpus = new SymbolCorpus();
            hitCorpus.dialogues = new SymbolDialogue[input.dialogues.Length];

            SymbolDialogue inputDialogue;
            SymbolSentence inputSentence;
            for (int i = 0; i < input.dialogues.Length; i++)
            {
                inputDialogue = input.dialogues[i];

                //Create same dimension hit dialogue
                hitCorpus.dialogues[i] = new SymbolDialogue();
                hitCorpus.dialogues[i].sentences = new SymbolSentence[input.dialogues[i].sentences.Length];

                for (int j = 0; j < inputDialogue.sentences.Length; j++)
                {
                    inputSentence = inputDialogue.sentences[j];

                    hitCorpus.dialogues[i].sentences[j] = new SymbolSentence();

                    //predict corpus, reset state per sentence
                    state = 0;
                    hitCorpus.dialogues[i].sentences[j].symbols = predictHits(inputSentence.symbols);
                }
            }

            return hitCorpus;
        }

        //Predicts with given control states and selfmade predictions.  Returning predictions
        public byte[] project(byte[] control)
        {
            byte[] output = new byte[control.Length];

            state = control[0];
            output[0] = predict(0);

            for (int i = 1; i < output.Length; i++)
            {
                state = control[i];
                output[i] = predict(output[i - 1]);
            }

            return output;
        }
        //Projects corpus
        public SymbolCorpus project(SymbolCorpus control)
        {
            //Create same dimension output Corpus
            SymbolCorpus outputCorpus = new SymbolCorpus();
            outputCorpus.dialogues = new SymbolDialogue[control.dialogues.Length];

            //Process
            SymbolDialogue controlDialogue;
            SymbolSentence controlSentence;
            for (int i = 0; i < control.dialogues.Length; i++)
            {
                controlDialogue = control.dialogues[i];

                //Create same dimension output dialogue
                outputCorpus.dialogues[i] = new SymbolDialogue();
                outputCorpus.dialogues[i].sentences = new SymbolSentence[control.dialogues[i].sentences.Length];

                for (int j = 0; j < controlDialogue.sentences.Length; j++)
                {
                    controlSentence = controlDialogue.sentences[j];

                    outputCorpus.dialogues[i].sentences[j] = new SymbolSentence();

                    //project corpus, reset state per sentence
                    state = 0;
                    outputCorpus.dialogues[i].sentences[j].symbols = project(controlSentence.symbols);
                }
            }

            return outputCorpus;
        }

        public string getDescription()
        {
            string stats = "";

            stats += "Training Depth: " + trainingDepth + "\n";
            stats += "Symbol size: " + symbolSize + "\n";
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
            stats += " :: s.entropy: " + ("" + stateEntropy).PadRight(10).Substring(0, 4);
            return stats;
        }

        public void train(SymbolCorpus trainSet, int cycles)
        {
            Random rdm = new Random();

            StatePredictor predictor = new StatePredictor(symbolSize, stateSize);
            //Make copy of current
            copyInto(this, predictor);

            if (predictor.trainingDepth == 0)
            {
                randomOverride(predictor, rdm);

                //Read and predict analysis
                predictor.testPredict(trainSet);

                //TODO Generate some more randoms and choose best - might not be needed since leap Intensity is so high at the beginning
            }

            //Initialize training variables
            StatePredictor testTable = new StatePredictor(predictor.symbolSize, predictor.stateSize);
            StatePredictor temp;

            double leapIntensity = predictor.lastLeapIntensity;
            for (int i = 0; i < cycles; i++)
            {
                //Mutation
                leapOverride(testTable, predictor, rdm, leapIntensity);

                //Read and predict analysis
                testTable.testPredict(trainSet);

                if (testTable.accuracy > predictor.accuracy)
                {
                    //Best predictor found

                    //Soft change with memory for sucessful mutations intensity (x2 because of random average behaviour)
                    leapIntensity = 0.8 * leapIntensity + 0.2 * (2 * testTable.lastLeapIntensity);

                    if (leapIntensity > 0.9)
                        leapIntensity = 0.9; //maximum allowed mutation intensity

                    //Swap predictors for memory usage
                    temp = predictor;
                    predictor = testTable;
                    testTable = temp;
                }
            }

            copyInto(predictor, this);
        }



        public void saveToFile(string file)
        {
            //version, symbol size, state size, species depth
            byte[] saveArray = new byte[1 + 4 + 4 + 4 + table.Length];

            //version
            saveArray[0] = 0;

            //symbol size
            byte[] bR;
            bR = BitConverter.GetBytes(symbolSize);
            bR.CopyTo(saveArray, 1);

            //state size
            bR = BitConverter.GetBytes(stateSize);
            bR.CopyTo(saveArray, 5);

            //species depth
            bR = BitConverter.GetBytes(trainingDepth);
            bR.CopyTo(saveArray, 9);

            //Table
            table.CopyTo(saveArray, 13);
            System.IO.File.WriteAllBytes(file, saveArray);
        }
        public static StatePredictor loadFromFile(string file)
        {
            byte[] saveArray = System.IO.File.ReadAllBytes(file);

            StatePredictor load = null;

            //version check
            if (saveArray[0] == 0)
            {
                int symbolSize = BitConverter.ToInt32(saveArray, 1);
                int stateSize = BitConverter.ToInt32(saveArray, 5);
                load = new StatePredictor(symbolSize, stateSize);
                load.trainingDepth = BitConverter.ToInt32(saveArray, 9);
                for (int i = 0; i < load.table.Length; i++)
                    load.table[i] = saveArray[13 + i];
            }
            else
            {
                throw new Exception("Invalid file version.");
            }

            return load;
        }

        public static void leapOverride(StatePredictor destination, StatePredictor original, Random rdm, double intensity)
        {
            destination.reset();
            //Copy original to destination
            original.table.CopyTo(destination.table, 0);

            destination.trainingDepth = original.trainingDepth + 1;

            //Choose the number of changes to make to the original based on intensity
            //not very optimal
            int changeCount = rdm.Next(2 + (int)(original.table.Length * intensity));
            destination.lastLeapCount = changeCount;

            //Make changes
            int index;
            for (int i = 0; i < changeCount; i++)
            {
                //Choose random table index for change
                index = rdm.Next(destination.table.Length);

                //Check if index is prediction image or state image
                if (index % 2 == 0)
                    destination.table[index] = (byte)rdm.Next(destination.symbolSize); //New random prediction at index
                else
                    destination.table[index] = (byte)rdm.Next(destination.stateSize); //New random state at index
            }
        }
        public static void randomOverride(StatePredictor destination, Random rdm)
        {
            destination.reset();
            destination.trainingDepth = 0;


            for (int i = 0; i < destination.table.Length; i++)
            {
                //Check if index is prediction image or state image
                if (i % 2 == 0)
                    destination.table[i] = (byte)rdm.Next(destination.symbolSize); //New random prediction at index
                else
                    destination.table[i] = (byte)rdm.Next(destination.stateSize); //New random state at index
            }
        }

        public static void copyInto(StatePredictor original, StatePredictor destination)
        {
            original.table.CopyTo(destination.table, 0);
            destination.errorCount = original.errorCount;
            destination.lastLeapCount = original.lastLeapCount;
            destination.predictionCount = original.predictionCount;
            destination.state = original.state;
            destination.stateSize = original.stateSize;
            destination.symbolSize = original.symbolSize;
            destination.tableStride = original.tableStride;
            destination.trainingDepth = original.trainingDepth;
            original.stateMetrics.CopyTo(destination.stateMetrics, 0);
        }
    }

}
