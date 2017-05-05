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

        public int symbolSize;
        public int stateSize;

        public byte[] predictionTable;
        public float[] predictionAccuracy;
        public short tableStride;

        public int[][] predictionAccumulationTable;

        public byte[] stateOnlyPredictionTable;
        public int[][] stateOnlyAccumulationTable;
        public float[] stateOnlyPredictionAccuracy;

        public byte[] inputOnlyPredictionTable;
        public int[][] inputOnlyAccumulationTable;
        public float[] inputOnlyPredictionAccuracy;


        public byte[] stateTransitionTable;
        public short stateStride;

        public byte state;

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
        public RestrictedStatePredictor(int symbolSize, int stateSize)
        {
            this.symbolSize = symbolSize;
            this.stateSize = stateSize;

            //Create table with input(symbolSize , stateSize) each with 2 entries ( prediction , newState )
            predictionTable = new byte[symbolSize * stateSize];
            //Stride, bytes per symbol (states + (prediction , newState))
            tableStride = (short)(stateSize); // stateSize times 2 output bytes
            stateMetrics = new int[stateSize];
            //i0 , s0: p00 , s00
            //i0 , s1: p01 , s01
            //...

            //Create state transition table based on what was seen and what should have been the prediction ( input, correct)
            stateTransitionTable = new byte[symbolSize * stateSize];
            stateStride = (short)stateSize;


            //Help structures
            predictionAccuracy = new float[symbolSize * stateSize];

            predictionAccumulationTable = new int[symbolSize * stateSize][];
            for (int i = 0; i < predictionAccumulationTable.Length; i++)
                predictionAccumulationTable[i] = new int[symbolSize];

            stateOnlyPredictionTable = new byte[stateSize];
            stateOnlyPredictionAccuracy = new float[stateSize];
            stateOnlyAccumulationTable = new int[stateSize][];
            for (int i = 0; i < stateSize; i++)
                stateOnlyAccumulationTable[i] = new int[symbolSize];

            inputOnlyPredictionTable = new byte[symbolSize];
            inputOnlyPredictionAccuracy = new float[symbolSize];
            inputOnlyAccumulationTable = new int[symbolSize][];
            for (int i = 0; i < symbolSize; i++)
                inputOnlyAccumulationTable[i] = new int[symbolSize];
        }
        
        public void reset()
        {
            state = 0;
            predictionCount = 0;
            errorCount = 0;
            for (int i = 0; i < stateMetrics.Length; i++)
                stateMetrics[i] = 0;
        }

        

        public void testPredict(byte[] input)
        {
            int errors = 0;

            byte prediction;
            //Make first prediction, it doesn't have input (default 0 which is the void '_')
            //state = 0;
            //prediction = predict(0);
            stateMetrics[state]++;
            int index = (0 * tableStride + state);
            prediction = predictionTable[index];

            if (prediction != input[0])
                errors++;

            state = stateTransitionTable[index];


            //Make the rest of the predictions
            for (int i = 1; i < input.Length; i++)
            {
                //prediction = predict(input[i - 1]);
                stateMetrics[state]++;
                index = (input[i - 1] * tableStride + state);

                prediction = predictionTable[index];

                if (prediction != input[i])
                    errors++;

                state = stateTransitionTable[index];
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
                //reset state per dialogue
                state = 0;
                inputDialogue = input.dialogues[i];

                for (int j = 0; j < inputDialogue.sentences.Length; j++)
                {
                    inputSentence = inputDialogue.sentences[j];


                    testPredict(inputSentence.symbols);
                }
            }
        }


        //increments optimal prediction table
        public void bake(byte input, byte expected)
        {
            stateMetrics[state]++;

            int index = (input * tableStride + state);
            predictionAccumulationTable[index][expected]++; //Increment frequency

            state = stateTransitionTable[index]; //Transition
        }

        //increments optimal prediction table
        public void bake(byte[] input)
        {
            stateMetrics[state]++;

            int index = (0 * tableStride + state);
            predictionAccumulationTable[index][input[0]]++; //Increment frequency

            state = stateTransitionTable[index]; //Transition


            //Make the rest of the predictions
            for (int i = 1; i < input.Length; i++)
            {
                //prediction = predict(input[i - 1]);
                stateMetrics[state]++;

                index = (input[i - 1] * tableStride + state);
                predictionAccumulationTable[index][input[i]]++;

                state = stateTransitionTable[index];
            }
        }
        //Creates optimal prediction table based on current state transition table
        public void bake(SymbolCorpus input)
        {
            SymbolDialogue inputDialogue;
            SymbolSentence inputSentence;
            for (int i = 0; i < input.dialogues.Length; i++)
            {
                //reset state per dialogue
                state = 0;
                inputDialogue = input.dialogues[i];

                for (int j = 0; j < inputDialogue.sentences.Length; j++)
                {
                    inputSentence = inputDialogue.sentences[j];


                    bake(inputSentence.symbols);
                }
            }
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

                for (int j = 0; j < symbolSize; j++)
                {
                    wIndex = j * tableStride + i;

                    bestCount = predictionAccumulationTable[wIndex][0];
                    stateOnlyAccumulationTable[i][0] += predictionAccumulationTable[wIndex][0];
                    inputOnlyAccumulationTable[j][0] += predictionAccumulationTable[wIndex][0];
                    predictionAccumulationTable[wIndex][0] = 0;
                    bestSymbol = 0;
                    predCount = bestCount;
                    for (byte k = 1; k < symbolSize; k++)
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
                for (byte k = 1; k < symbolSize; k++)
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
            for (int i = 0; i < symbolSize; i++)
            {
                bestCount = inputOnlyAccumulationTable[i][0];
                bestSymbol = 0;
                predCount = bestCount;
                inputOnlyAccumulationTable[i][0] = 0;
                for (byte k = 1; k < symbolSize; k++)
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
            for (int i = 0; i < symbolSize; i++)
            {
                for (int j = 0; j < stateSize; j++)
                {
                    wIndex = i * tableStride + j;

                    //If predictionTable is invalid on element
                    if (predictionAccuracy[wIndex] == 0)
                    {
                        //Use input only
                        if (inputOnlyPredictionAccuracy[i] > stateOnlyPredictionAccuracy[j])
                        {
                            predictionTable[wIndex] = inputOnlyPredictionTable[i];
                            predictionAccuracy[wIndex] = inputOnlyPredictionAccuracy[i];
                        }
                        else //Use state only
                        {
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
        //Process to retrive predictions, hits and states
        public ProcessResult process(byte[] input)
        {
            ProcessResult result = new ProcessResult();
            result.hits = new byte[input.Length];
            result.predictions = new byte[input.Length];
            result.states = new byte[input.Length];

            //Make first prediction and store it, it doesn't have input (default 0)
            //state = 0;
            //result.predictions[0] = predict(0);
            //stateMetrics[state]++;
            int index = (0 * tableStride + state);
            result.predictions[0] = predictionTable[index];

            //Check if it was mistaken
            if (result.predictions[0] != input[0])
                result.hits[0] = 0;
            else
                result.hits[0] = 1;

            state = stateTransitionTable[index];
            result.states[0] = state;

            //Make the rest of the predictions
            for (int i = 1; i < input.Length; i++)
            {
                //result.predictions[i] = predict(input[i - 1]);
                //stateMetrics[state]++;
                index = (input[i - 1] * tableStride + state);
                result.predictions[i] = predictionTable[index];

                if (result.predictions[i] != input[i])
                    result.hits[i] = 0;
                else
                    result.hits[i] = 1;

                state = stateTransitionTable[index];
                result.states[i] = state;
            }

            return result;
        }
        public CorpusResult process(SymbolCorpus input)
        {
            CorpusResult result = new CorpusResult();

            //Create same dimension process Corpus
            result.states = new SymbolCorpus();
            result.states.dialogues = new SymbolDialogue[input.dialogues.Length];
            result.hits = new SymbolCorpus();
            result.hits.dialogues = new SymbolDialogue[input.dialogues.Length];
            result.predictions = new SymbolCorpus();
            result.predictions.dialogues = new SymbolDialogue[input.dialogues.Length];

            //Process
            SymbolDialogue inputDialogue;
            SymbolSentence inputSentence;
            ProcessResult sentenceResult;
            for (int i = 0; i < input.dialogues.Length; i++)
            {
                inputDialogue = input.dialogues[i];

                //Create same dimension process dialogue
                result.states.dialogues[i] = new SymbolDialogue();
                result.states.dialogues[i].sentences = new SymbolSentence[input.dialogues[i].sentences.Length];
                result.hits.dialogues[i] = new SymbolDialogue();
                result.hits.dialogues[i].sentences = new SymbolSentence[input.dialogues[i].sentences.Length];
                result.predictions.dialogues[i] = new SymbolDialogue();
                result.predictions.dialogues[i].sentences = new SymbolSentence[input.dialogues[i].sentences.Length];

                state = 0;
                for (int j = 0; j < inputDialogue.sentences.Length; j++)
                {
                    inputSentence = inputDialogue.sentences[j];

                    result.states.dialogues[i].sentences[j] = new SymbolSentence();
                    result.hits.dialogues[i].sentences[j] = new SymbolSentence();
                    result.predictions.dialogues[i].sentences[j] = new SymbolSentence();

                    //predict corpus
                    sentenceResult = process(inputSentence.symbols);
                    result.states.dialogues[i].sentences[j].symbols = sentenceResult.states;
                    result.hits.dialogues[i].sentences[j].symbols = sentenceResult.hits;
                    result.predictions.dialogues[i].sentences[j].symbols = sentenceResult.predictions;
                }
            }

            return result;
        }

        public byte[] response(SymbolDialogue dialogue)
        {
            //Reset state
            state = 0;

            //Process all current sentences
            SymbolSentence inputSentence;
            for (int j = 0; j < dialogue.sentences.Length; j++)
            {
                inputSentence = dialogue.sentences[j];

                //predict corpus
                process(inputSentence.symbols);
            }

            //Return cascade from last state
            return cascade(state, 128);
        }

        //Predicts with given control states and selfmade predictions.  Returning predictions
        public byte[] project(byte[] control)
        {
            byte[] output = new byte[control.Length];

            state = control[0];
            int index = (0 * tableStride + state);
            output[0] = predictionTable[index];

            for (int i = 1; i < output.Length; i++)
            {
                state = control[i];

                index = (output[i - 1] * tableStride + state);
                output[i] = predictionTable[index];
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

        //cascade from one state
        public byte[] cascade(byte startState, int maxLength = 128)
        {
            List<byte> output = new List<byte>();
            state = startState;

            int index = (0 * tableStride + state);
            output.Add(predictionTable[index]);
            state = stateTransitionTable[index];

            byte single;
            for (int i = 1; i < maxLength; i++)
            {
                //output[i] = predict(output[i - 1]);
                index = (output[i - 1] * tableStride + state);
                single = predictionTable[index];

                //Break if AI prints voidChar
                if (single == 0)
                    break;

                output.Add(single);
                state = stateTransitionTable[index];
            }

            return output.ToArray();
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
            stats += " :: pc.: " + predictionCount;
            stats += " :: s.entropy: " + ("" + stateEntropy).PadRight(10).Substring(0, 4);
            return stats;
        }

        [NonSerialized]
        Trainer trainer;
        public void StartTrain(SymbolCorpus trainSet)
        {
            trainer = new Trainer(this);
            trainer.StartTrain(trainSet);
        }
        public void StopTrain()
        {
            trainer.StopTrain();
        }

        public class ProcessResult
        {
            public byte[] predictions;
            public byte[] hits;
            public byte[] states;
        }
        public class CorpusResult
        {
            public SymbolCorpus predictions;
            public SymbolCorpus hits;
            public SymbolCorpus states;
        }

        public static void leapOverride(RestrictedStatePredictor original, RestrictedStatePredictor destination, Random rdm, int intensity)
        {
            destination.reset();
            //Copy original to destination
            original.stateTransitionTable.CopyTo(destination.stateTransitionTable, 0);

            destination.trainingDepth = original.trainingDepth + 1;

            //Choose the number of changes to make to the original based on intensity
            //not very optimal
            int changeCount = intensity;

            //Make changes
            int index;
            for (int i = 0; i < changeCount; i++)
            {
                //Choose random table index for change
                index = rdm.Next(destination.stateTransitionTable.Length);

                //Check if index is prediction image or state image
                destination.stateTransitionTable[index] = (byte)rdm.Next(destination.stateSize); //New random state at index
            }
        }
        public static void randomOverride(RestrictedStatePredictor destination, Random rdm)
        {
            destination.reset();
            destination.trainingDepth = 0;


            for (int i = 0; i < destination.stateTransitionTable.Length; i++)
            {
                destination.stateTransitionTable[i] = (byte)rdm.Next(destination.stateSize); //New random state at index
            }
        }

        public static void copyInto(RestrictedStatePredictor original, RestrictedStatePredictor destination)
        {
            original.predictionTable.CopyTo(destination.predictionTable, 0);
            destination.tableStride = original.tableStride;
            destination.errorCount = original.errorCount;
            destination.predictionCount = original.predictionCount;
            destination.state = original.state;
            destination.stateSize = original.stateSize;
            destination.symbolSize = original.symbolSize;
            destination.trainingDepth = original.trainingDepth;
            original.stateMetrics.CopyTo(destination.stateMetrics, 0);
            original.stateTransitionTable.CopyTo(destination.stateTransitionTable, 0);
            destination.stateStride = original.stateStride;
        }

        public class Trainer
        {
            RestrictedStatePredictor target;

            public bool training = false;

            RestrictedStatePredictor best;
            object checkLock;
            List<Thread> threads;
            List<object> readLocks;
            List<object> trainLocks;
            List<RestrictedStatePredictor> predictors;

            SymbolCorpus trainSet;


            int[] trainIntensity;
            int[] trainSampleCount;
            double[] trainAccumulatedAccuracies;


            int dnaLength;

            public Trainer(RestrictedStatePredictor target)
            {
                this.target = target;
                checkLock = new object();
                threads = new List<Thread>();
                readLocks = new List<object>();
                trainLocks = new List<object>();
                predictors = new List<RestrictedStatePredictor>();

                dnaLength = target.stateTransitionTable.Length;
                int steps;
                for (steps = 0; Math.Pow(2, steps) < dnaLength; steps++) ;
                steps++; //include last and first
                trainIntensity = new int[steps];
                trainSampleCount = new int[steps];
                trainAccumulatedAccuracies = new double[steps];
                for (int i = 0; i < steps; i++)
                {
                    if (i != steps - 1)
                        trainIntensity[i] = (int)Math.Pow(2, i);
                    else
                        trainIntensity[i] = dnaLength;
                }
            }

            public void StartTrain(SymbolCorpus trainSet)
            {
                this.trainSet = trainSet;
                int threadCount = 8;
                training = true;

                best = new RestrictedStatePredictor(target.symbolSize, target.stateSize);
                RestrictedStatePredictor.copyInto(target, best);
                best.reset();
                best.bake(trainSet);
                Console.WriteLine("Layer training started with: " + best.getStats());

                for (int i = 0; i < trainSampleCount.Length; i++)
                {
                    trainSampleCount[i] = 1;
                    trainAccumulatedAccuracies[i] = best.accuracy;
                }


                for (int i = 0; i < threadCount; i++)
                {
                    //Add thread lock
                    readLocks.Add(new object());
                    trainLocks.Add(new object());
                    //Create thread resource
                    RestrictedStatePredictor tester = new RestrictedStatePredictor(best.symbolSize, best.stateSize);
                    predictors.Add(tester);
                    //Create and start thread
                    Thread tt = new Thread(ThreadTrain);
                    threads.Add(tt);
                }

                //Start threads after to avoid first cycle conflicts
                for (int i = 0; i < threadCount; i++)
                    threads[i].Start(i);
            }

            private void printTrainingIntensityStatus()
            {
                Console.WriteLine("Train Intensity Status: ");
                string line;
                for (int i = 0; i < trainIntensity.Length; i++)
                {
                    line = "";
                    line += ("" + trainIntensity[i]).PadRight(10).Substring(0, 8);
                    line += " :: samples: " + ("" + trainSampleCount[i]).PadRight(10).Substring(0, 8);
                    line += " :: avg.Gain: " + ("" + (trainAccumulatedAccuracies[i] / trainSampleCount[i]).ToString("0." + new string('#', 339)));
                    Console.WriteLine(line);
                }
            }

            private void ThreadTrain(object indexObj)
            {
                int index = (int)indexObj;
                Random rdm = new Random(index);
                int currentTrainIndex = trainIntensity.Length - 1;

                Monitor.Enter(trainLocks[index]);


                int usedIntensity;
                //while training is in place
                while (training)
                {
                    //Copy from best (if version control is in place, check version)
                    usedIntensity = rdm.Next(trainIntensity[currentTrainIndex]) + 1; //Inclusive upper bound, exclusive lower bound
                    lock (readLocks[index])
                    {
                        RestrictedStatePredictor.leapOverride(best, predictors[index], rdm, usedIntensity);
                    }

                    predictors[index].bake(trainSet);

                    lock (checkLock)
                    {
                        //Increment used training intensity sampling count
                        for (int i = currentTrainIndex; i >= 0; i--)
                        {
                            if (usedIntensity <= trainIntensity[i])
                            {
                                trainSampleCount[i]++;
                            }
                        }

                        if (predictors[index].accuracy > best.accuracy)
                        {
                            //Best found
                            //Tune training intensity
                            //Add incentive to used training Intensity
                            double bestImprovement = trainAccumulatedAccuracies[0] / trainSampleCount[0];
                            int bestIndex = 0;
                            double improvement;
                            for (int i = currentTrainIndex; i >= 0; i--)
                            {
                                if (usedIntensity <= trainIntensity[i])
                                {
                                    trainAccumulatedAccuracies[i] += predictors[index].accuracy - best.accuracy;
                                }
                                improvement = trainAccumulatedAccuracies[i] / trainSampleCount[i];
                                if (improvement > bestImprovement)
                                {
                                    bestImprovement = improvement;
                                    bestIndex = i;
                                }
                            }
                            //Go through remaining intensitys to check if there is a better one
                            for (int i = currentTrainIndex + 1; i < trainSampleCount.Length; i++)
                            {
                                improvement = trainAccumulatedAccuracies[i] / trainSampleCount[i];
                                if (improvement > bestImprovement)
                                {
                                    bestImprovement = improvement;
                                    bestIndex = i;
                                }
                            }
                            //Change index to the best expected improvement
                            currentTrainIndex = bestIndex;

                            //if (predictors[index].trainingDepth % 10 == 0)
                            //    printTrainingIntensityStatus();

                            //Start copy procedure
                            //Lock all read locks
                            for (int i = 0; i < readLocks.Count; i++)
                            {
                                Monitor.Enter(readLocks[i]);
                            }

                            //Copy into best
                            RestrictedStatePredictor.copyInto(predictors[index], best);

                            Console.WriteLine("Leap by thread " + index + " : " + best.getStats());
                            //Unlock all read locks
                            for (int i = 0; i < readLocks.Count; i++)
                            {
                                Monitor.Exit(readLocks[i]);
                            }
                        }
                        else
                        {
                            //Failed leap
                            //leapIntensity = 0.99 * leapIntensity + 0.01 * (1f); //Gravity to randomness
                        }
                    }
                }

                Monitor.Exit(trainLocks[index]);
            }

            public void StopTrain()
            {
                training = false;

                //Pass through all trainLocks to check cleared
                for (int i = 0; i < trainLocks.Count; i++)
                {
                    Monitor.Enter(trainLocks[i]);
                    Monitor.Exit(trainLocks[i]);
                }

                //Copy train result to target
                RestrictedStatePredictor.copyInto(best, target);
            }
        }
    }
}
