using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public short tableStride;

        public byte[] stateTransitionTable;
        public bool[] lossTransitionTable;
        public short stateStride;

        public byte state;

        public int predictionCount;
        public int errorCount;
        public int lossCount;
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
        public float loss
        {
            get { return lossCount / (float)predictionCount; }
        }

        private HybridStatePredictor()
        {

        }
        public HybridStatePredictor(int symbolSize, int stateSize)
        {
            this.symbolSize = symbolSize;
            this.stateSize = stateSize;

            //Create table with input(symbolSize , stateSize) each with 2 entries ( prediction , newState )
            predictionTable = new byte[symbolSize * stateSize * 2];
            //Stride, bytes per symbol (states + (prediction , newState))
            tableStride = (short)(stateSize * 2); // stateSize times 2 output bytes
            stateMetrics = new int[stateSize];
            //i0 , s0: p00 , s00
            //i0 , s1: p01 , s01
            //...

            //Create state transition table based on what was seen and what should have been the prediction ( input, correct)
            stateTransitionTable = new byte[symbolSize * symbolSize];
            stateStride = (short)symbolSize;
            lossTransitionTable = new bool[symbolSize * symbolSize];

            lastLeapCount = (int)(predictionTable.Length * 0.99f);

            computeStateTransitionsTable();
        }

        void computeStateTransitionsTable()
        {
            int matchState;
            bool loss;
            for (int i = 0; i < symbolSize; i++)
            {
                for (int j = 0; j < symbolSize; j++)
                {
                    loss = true;
                    matchState = stateSize - 1;
                    for (int k = 0; k < stateSize; k++)
                    {
                        if (predictionTable[i * tableStride + k * 2 + 0] == (byte)j)
                        {
                            matchState = k;
                            loss = false;
                            break;
                        }
                    }
                    stateTransitionTable[i * stateStride + j] = (byte)matchState;
                    lossTransitionTable[i * stateStride + j] = loss;
                }
            }
        }

        public void reset()
        {
            state = 0;
            predictionCount = 0;
            errorCount = 0;
            lossCount = 0;
            for (int i = 0; i < stateMetrics.Length; i++)
                stateMetrics[i] = 0;
        }

        ////Make a single prediction and update state
        //public byte predict(byte input)
        //{
        //    //Increment one prediction made with current state
        //    stateMetrics[state]++;

        //    //Object index
        //    int index = (input * tableStride + state * 2);

        //    //Return prediction
        //    return table[index + 0];
        //}

        //Predicts only to update error

        public void testPredict(byte[] input)
        {
            int errors = 0;

            byte prediction;
            //Make first prediction, it doesn't have input (default 0 which is the void '_')
            //state = 0;
            //prediction = predict(0);
            stateMetrics[state]++;
            int index = (0 * tableStride + state * 2);
            prediction = predictionTable[index + 0];
            if (prediction != input[0])
            {
                errors++;
                state = stateTransitionTable[0 * stateStride + input[0]];
                lossCount += lossTransitionTable[0 * stateStride + input[0]] ? 1 : 0;
            }
            else
                state = predictionTable[index + 1];


            //Make the rest of the predictions
            for (int i = 1; i < input.Length; i++)
            {
                //prediction = predict(input[i - 1]);
                stateMetrics[state]++;
                index = (input[i - 1] * tableStride + state * 2);
                prediction = predictionTable[index + 0];

                if (prediction != input[i])
                {
                    errors++;
                    state = stateTransitionTable[input[i - 1] * stateStride + input[i]];
                    lossCount += lossTransitionTable[input[i - 1] * stateStride + input[i]] ? 1 : 0;
                }
                else
                    state = predictionTable[index + 1];
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
            int index = (0 * tableStride + state * 2);
            result.predictions[0] = predictionTable[index + 0];

            //Check if it was mistaken
            if (result.predictions[0] != input[0])
            {
                state = stateTransitionTable[0 * stateStride + input[0]];
                //Store state
                result.states[0] = state; //State was invalid, correct it

                result.hits[0] = (lossTransitionTable[0 * stateStride + input[0]]) ? (byte)(symbolSize - 1) : (byte)0;
            }
            else
            {
                //Store state
                result.states[0] = state; //State was valid, keep it

                state = predictionTable[index + 1];
                result.hits[0] = 1;
            }

            //Make the rest of the predictions
            for (int i = 1; i < input.Length; i++)
            {
                //result.predictions[i] = predict(input[i - 1]);
                //stateMetrics[state]++;
                index = (input[i - 1] * tableStride + state * 2);
                result.predictions[i] = predictionTable[index + 0];

                if (result.predictions[i] != input[i])
                {
                    state = stateTransitionTable[input[i - 1] * stateStride + input[i]];
                    result.states[i] = state;//State was invalid, correct it

                    result.hits[i] = (lossTransitionTable[input[i - 1] * stateStride + input[i]]) ? (byte)(symbolSize - 1) : (byte)0;
                }
                else
                {
                    result.states[i] = state;
                    state = predictionTable[index + 1];

                    result.hits[i] = 1;
                }
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
            //output[0] = predict(0);
            int index = (0 * tableStride + state * 2);
            output[0] = predictionTable[index + 0];

            for (int i = 1; i < output.Length; i++)
            {
                state = control[i];
                //output[i] = predict(output[i - 1]);
                index = (output[i - 1] * tableStride + state * 2);
                output[i] = predictionTable[index + 0];
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

            int index = (0 * tableStride + state * 2);
            output.Add(predictionTable[index + 0]);
            state = predictionTable[index + 1];

            byte single;
            for (int i = 1; i < maxLength; i++)
            {
                //output[i] = predict(output[i - 1]);
                index = (output[i - 1] * tableStride + state * 2);
                single = predictionTable[index + 0];

                //Break if AI prints voidChar
                if (single == 0)
                    break;

                output.Add(predictionTable[index + 0]);
                state = predictionTable[index + 1];
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
            stats += "Loss: " + loss + "\n";
            stats += "Leap: " + lastLeapIntensity + "\n";

            return stats;
        }
        public string getStats()
        {
            string stats = "";
            stats += " :: depth. " + (trainingDepth + "").PadRight(20).Substring(0, 6);
            stats += " :: acc.: " + accuracy;
            stats += " :: s.entropy: " + ("" + stateEntropy).PadRight(10).Substring(0, 4);
            stats += " :: loss: " + ("" + loss).PadRight(10).Substring(0, 5);
            stats += " :: lit: " + ("" + lastLeapIntensity).PadRight(10).Substring(0, 7);
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
        public void train2(SymbolCorpus trainSet, double minutes)
        {
            Random rdm = new Random();

            HybridStatePredictor predictor = new HybridStatePredictor(symbolSize, stateSize);
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
            HybridStatePredictor testTable = new HybridStatePredictor(predictor.symbolSize, predictor.stateSize);
            HybridStatePredictor temp;

            double leapIntensity = predictor.lastLeapIntensity * 2 * 4;
            if (leapIntensity > 0.9)
                leapIntensity = 0.9;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            Console.Write("Training:");
            int percentil = 1;
            while (stopwatch.ElapsedMilliseconds < 1000 * 60 * minutes)
            {
                if (stopwatch.ElapsedMilliseconds > (1000 * 60 * minutes / 10.0) * percentil)
                {
                    Console.Write("$"); //print training tick
                    percentil++;
                }

                //Mutation
                //leapOverride(testTable, predictor, rdm, leapIntensity);

                //Read and predict analysis
                testTable.testPredict(trainSet);

                if (testTable.accuracy > predictor.accuracy)
                {
                    //Best predictor found

                    //Soft change with memory for sucessful mutations intensity (x2 because of random average behaviour)
                    leapIntensity = 0.8 * leapIntensity + 0.2 * (2 * testTable.lastLeapIntensity * 4);

                    if (leapIntensity > 0.9)
                        leapIntensity = 0.9; //maximum allowed mutation intensity

                    //Swap predictors for memory usage
                    temp = predictor;
                    predictor = testTable;
                    testTable = temp;
                }
            }
            stopwatch.Stop();
            Console.WriteLine();

            copyInto(predictor, this);
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

        public void saveToFile(string file)
        {
            //version, symbol size, state size, species depth
            byte[] saveArray = new byte[1 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + predictionTable.Length];

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

            //predictionCount
            bR = BitConverter.GetBytes(predictionCount);
            bR.CopyTo(saveArray, 13);

            //errorCount
            bR = BitConverter.GetBytes(errorCount);
            bR.CopyTo(saveArray, 17);

            //lossCount
            bR = BitConverter.GetBytes(lossCount);
            bR.CopyTo(saveArray, 21);

            //lastLeapCount
            bR = BitConverter.GetBytes(lastLeapCount);
            bR.CopyTo(saveArray, 25);

            //Table
            predictionTable.CopyTo(saveArray, 29);
            System.IO.File.WriteAllBytes(file, saveArray);
        }
        public static HybridStatePredictor loadFromFile(string file)
        {
            byte[] saveArray = System.IO.File.ReadAllBytes(file);

            HybridStatePredictor load = null;

            //version check
            if (saveArray[0] == 0)
            {
                int symbolSize = BitConverter.ToInt32(saveArray, 1);
                int stateSize = BitConverter.ToInt32(saveArray, 5);
                load = new HybridStatePredictor(symbolSize, stateSize);
                load.trainingDepth = BitConverter.ToInt32(saveArray, 9);
                load.predictionCount = BitConverter.ToInt32(saveArray, 13);
                load.errorCount = BitConverter.ToInt32(saveArray, 17);
                load.lossCount = BitConverter.ToInt32(saveArray, 21);
                load.lastLeapCount = BitConverter.ToInt32(saveArray, 25);
                for (int i = 0; i < load.table.Length; i++)
                    load.table[i] = saveArray[29 + i];

                load.computeStateTransitionsTable();
            }
            else
            {
                throw new Exception("Invalid file version.");
            }

            return load;
        }

        public static void leapOverride(HybridStatePredictor destination, HybridStatePredictor original, Random rdm, int intensity)
        {
            destination.reset();
            //Copy original to destination
            original.table.CopyTo(destination.table, 0);

            destination.trainingDepth = original.trainingDepth + 1;

            //Choose the number of changes to make to the original based on intensity
            //not very optimal
            int changeCount = intensity;
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

            destination.computeStateTransitionsTable();
        }
        public static void randomOverride(HybridStatePredictor destination, Random rdm)
        {
            destination.reset();
            destination.trainingDepth = 0;


            for (int i = 0; i < destination.table.Length; i++)
            {
                if (i % 2 == 0)
                    destination.table[i] = (byte)rdm.Next(destination.symbolSize); //New random prediction at index
                else
                    destination.table[i] = (byte)rdm.Next(destination.stateSize); //New random state at index
            }

            destination.computeStateTransitionsTable();
        }

        public static void copyInto(HybridStatePredictor original, HybridStatePredictor destination)
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
            destination.stateStride = original.stateStride;
            original.stateTransitionTable.CopyTo(destination.stateTransitionTable, 0);
            destination.lossCount = original.lossCount;
            original.lossTransitionTable.CopyTo(destination.lossTransitionTable, 0);
        }

        public class Trainer
        {
            HybridStatePredictor target;

            public bool training = false;

            HybridStatePredictor best;
            object checkLock;
            List<Thread> threads;
            List<object> readLocks;
            List<object> trainLocks;
            List<HybridStatePredictor> predictors;

            SymbolCorpus trainSet;


            int[] trainIntensity;
            int[] trainSampleCount;
            double[] trainAccumulatedAccuracies;


            int dnaLength;

            public Trainer(HybridStatePredictor target)
            {
                this.target = target;
                checkLock = new object();
                threads = new List<Thread>();
                readLocks = new List<object>();
                trainLocks = new List<object>();
                predictors = new List<HybridStatePredictor>();

                dnaLength = target.table.Length;
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

                best = new HybridStatePredictor(target.symbolSize, target.stateSize);
                HybridStatePredictor.copyInto(target, best);
                best.reset();
                best.testPredict(trainSet);
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
                    HybridStatePredictor tester = new HybridStatePredictor(best.symbolSize, best.stateSize);
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
                        HybridStatePredictor.leapOverride(predictors[index], best, rdm, usedIntensity);
                    }

                    predictors[index].testPredict(trainSet);

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

                            if (predictors[index].trainingDepth % 10 == 0)
                                printTrainingIntensityStatus();

                            //Start copy procedure
                            //Lock all read locks
                            for (int i = 0; i < readLocks.Count; i++)
                            {
                                Monitor.Enter(readLocks[i]);
                            }

                            //Copy into best
                            HybridStatePredictor.copyInto(predictors[index], best);

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
                HybridStatePredictor.copyInto(best, target);
            }
        }
    }
}
