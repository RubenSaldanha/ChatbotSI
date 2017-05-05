using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ChatbotSI
{
    //TODO
    //Vários conjuntos de testes (conversas p.ex)
    //CUDA - Threading
    //Restrictive predictor
    //TODO: Reverse predictor - might not be needed
    //TODO: Cascade function on StatePredictor
    //TODO Response algorithm (half-immediate, vs , generative)

    [Serializable()]
    public class Couppy
    {
        public int trainingDepth;
        public string name;

        public RestrictedStatePredictor inputLayer;

        [NonSerialized]
        public StatePredictor[] deepLayers;


        public int[] layerSizes;
        //public bool training;

        //TODO: ResponseNet

        public int LayerCount
        {
            get { return deepLayers.Length + 1; }
        }

        public Couppy()
        {

        }
        public Couppy(int[] layerSizes)
        {
            this.layerSizes = layerSizes;
            int symbolSize = Translator.SymbolCount;

            if (symbolSize > 256)
                throw new NotImplementedException("Invalid symbol size, must be between 1 and 256");

            for(int i=0;i<layerSizes.Length;i++)
                if (layerSizes[i] > 256)
                    throw new NotImplementedException("Invalid state size, must be between 1 and 256");

            inputLayer = new RestrictedStatePredictor(symbolSize, layerSizes[0]);

            if (layerSizes.Length > 1)
            {
                deepLayers = new StatePredictor[layerSizes.Length - 1];
                for (int i = 0; i < layerSizes.Length - 1; i++)
                    deepLayers[i] = new StatePredictor(layerSizes[i], layerSizes[i + 1]);
            }
        }

        private void bake(SymbolCorpus trainSet)
        {
            SymbolDialogue dialogue;
            SymbolSentence sentence;
            byte feed = 0;
            for (int i = 0; i < trainSet.dialogues.Length; i++)
            {
                dialogue = trainSet.dialogues[i];

                //Reset states
                if(deepLayers != null)
                    for (int k = 0; k < deepLayers.Length; k++)
                        deepLayers[k].state = 0;
                inputLayer.state = 0;

                for (int j = 0; j < dialogue.sentences.Length; j++)
                {
                    sentence = dialogue.sentences[j];

                    //Predict first symbol
                    if (deepLayers != null)
                    {
                        feed = deepLayers[deepLayers.Length - 1].state; //Use last state on last layer
                        for (int k = deepLayers.Length - 1; k >= 0; k--)
                        {
                            deepLayers[k].state = feed;
                            if (k != 0)
                                feed = deepLayers[k].predict(deepLayers[k - 1].state);
                            else
                                feed = deepLayers[k].predict(inputLayer.state);
                        }
                    }
                    else
                        feed = inputLayer.state;

                    inputLayer.state = feed;
                    inputLayer.bake(0, sentence.symbols[0]);

                    //Predict remaining symbols
                    for (int l = 1; l < sentence.symbols.Length; l++)
                    {
                        if (deepLayers != null)
                        {
                            feed = deepLayers[deepLayers.Length - 1].state; //Use last state on last layer
                            for (int k = deepLayers.Length - 1; k >= 0; k--)
                            {
                                deepLayers[k].state = feed;
                                if (k != 0)
                                    feed = deepLayers[k].predict(deepLayers[k - 1].state);
                                else
                                    feed = deepLayers[k].predict(inputLayer.state);
                            }
                        }
                        else
                            feed = inputLayer.state;

                        inputLayer.state = feed;
                        inputLayer.bake(sentence.symbols[l - 1], sentence.symbols[l]);
                    }
                }
            }

            inputLayer.finalizeBake();
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

        public byte[] response(SymbolDialogue dialogue)
        {
            SymbolSentence sentence;
            byte feed = 0;
            byte single;
            //Reset states
            if (deepLayers != null)
                for (int k = 0; k < deepLayers.Length; k++)
                    deepLayers[k].state = 0;

            inputLayer.state = 0;

            for (int j = 0; j < dialogue.sentences.Length; j++)
            {
                sentence = dialogue.sentences[j];

                //Predict first symbol
                if (deepLayers != null)
                {
                    feed = deepLayers[deepLayers.Length - 1].state; //Use last state on last layer
                    for (int k = deepLayers.Length - 1; k >= 0; k--)
                    {
                        deepLayers[k].state = feed;
                        if (k != 0)
                            feed = deepLayers[k].predict(deepLayers[k - 1].state);
                        else
                            feed = deepLayers[k].predict(inputLayer.state);
                    }
                }
                else
                    feed = inputLayer.state;


                inputLayer.state = feed;
                inputLayer.process(0);

                //Predict remaining symbols
                for (int l = 0; l < sentence.symbols.Length - 1; l++)
                {
                    if (deepLayers != null)
                    {
                        feed = deepLayers[deepLayers.Length - 1].state; //Use last state on last layer
                        for (int k = deepLayers.Length - 1; k >= 0; k--)
                        {
                            deepLayers[k].state = feed;
                            if (k != 0)
                                feed = deepLayers[k].predict(deepLayers[k - 1].state);
                            else
                                feed = deepLayers[k].predict(inputLayer.state);
                        }
                    }
                    else
                        feed = inputLayer.state;

                    inputLayer.state = feed;
                    inputLayer.process(sentence.symbols[l]);
                }
            }

            //Cascade
            List<byte> output = new List<byte>();
            int maxLength = 128;
            for (int i = 0; i < maxLength; i++)
            {
                if (deepLayers != null)
                {
                    feed = deepLayers[deepLayers.Length - 1].state; //Use last state on last layer
                    for (int k = deepLayers.Length - 1; k >= 0; k--)
                    {
                        deepLayers[k].state = feed;
                        if (k != 0)
                            feed = deepLayers[k].predict(deepLayers[k - 1].state);
                        else
                            feed = deepLayers[k].predict(inputLayer.state);
                    }
                }
                else
                    feed = inputLayer.state;

                inputLayer.state = feed;

                if (i != 0)
                    single = inputLayer.process(output[i - 1]);
                else
                    single = inputLayer.process(0);

                if (single == 0)
                    break;

                output.Add(single);
            }

            return output.ToArray();
        }

        public void SaveToFile(string file)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Couppy));
            TextWriter writer = new StreamWriter(file);
            serializer.Serialize(writer, this);
            writer.Close();
        }

        public static Couppy LoadFromFile(string file)
        {
            //Initialize deserializer
            XmlSerializer serializer = new XmlSerializer(typeof(Couppy));
            FileStream ReadFileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            Couppy loadedChatbot = (Couppy)serializer.Deserialize(ReadFileStream);
            ReadFileStream.Close();

            return loadedChatbot;
        }

        public string getDescription()
        {
            String description = "";

            description += "Layers :   ";
            for (int i = 0; i < layerSizes.Length; i++)
                description += "" + layerSizes[i] + "  ";
            description += "  (" + layerSizes.Length + ")\n";
            description += "Predictions: " + inputLayer.predictionCount + "     Accuracy: " + inputLayer.accuracy;
            return description;
        }

        public int getDnaLength()
        {
            int dnaLength = inputLayer.symbolSize * inputLayer.stateSize;

            if(deepLayers != null)
                for (int i=0;i<deepLayers.Length;i++)
                    dnaLength += deepLayers[i].symbolSize * deepLayers[i].stateSize * 2;


            return dnaLength;
        }

        public void printCascade()
        {
            Console.WriteLine("Printing cascade");

            int topLayer;
            int states;
            if (deepLayers != null)
            {
                topLayer = deepLayers.Length;
                states = inputLayer.stateSize;

                //Set maximum states to all possible combinations between layers
                for (int k = 0; k < deepLayers.Length; k++)
                    states *= deepLayers[k].stateSize;
            }
            else
            {
                topLayer = 0;
                states = inputLayer.stateSize;
            }

            byte single;
            byte feed;
            int iState;
            for (int i = 0; i < states; i++)
            {
                List<byte> output = new List<byte>();

                //Set Cascading States
                inputLayer.state = (byte)(i % inputLayer.stateSize);
                iState = i / inputLayer.stateSize;
                if (deepLayers != null)
                {
                    for (int k = 0; k < deepLayers.Length; k++)
                    {
                        deepLayers[k].state = (byte)(iState % deepLayers[k].stateSize);
                        iState = iState / deepLayers[k].stateSize;
                    }
                }


                if (deepLayers != null)
                {
                    feed = deepLayers[deepLayers.Length - 1].state; //Use last state on last layer
                    for (int k = deepLayers.Length - 1; k >= 0; k--)
                    {
                        deepLayers[k].state = feed;
                        if (k != 0)
                            feed = deepLayers[k].predict(deepLayers[k - 1].state);
                        else
                            feed = deepLayers[k].predict(inputLayer.state);
                    }
                }
                else
                    feed = inputLayer.state;

                inputLayer.state = feed;
                single = inputLayer.process(0);

                if (single == 0)
                    continue;

                output.Add(single);

                for (int j = 1; j < 128; j++)
                {
                    if (deepLayers != null)
                    {
                        feed = deepLayers[deepLayers.Length - 1].state; //Use last state on last layer
                        for (int k = deepLayers.Length - 1; k >= 0; k--)
                        {
                            deepLayers[k].state = feed;
                            if (k != 0)
                                feed = deepLayers[k].predict(deepLayers[k - 1].state);
                            else
                                feed = deepLayers[k].predict(inputLayer.state);
                        }
                    }
                    else
                        feed = inputLayer.state;

                    inputLayer.state = feed;
                    single = inputLayer.process(output[j - 1]);

                    //Break if AI prints voidChar
                    if (single == 0)
                        break;

                    output.Add(single);
                }


                string stateString = "";
                iState = i;
                stateString += iState % inputLayer.stateSize;
                iState = i / inputLayer.stateSize;
                if (deepLayers != null)
                {
                    for (int k = 0; k < deepLayers.Length; k++)
                    {
                        stateString = iState % deepLayers[k].stateSize + " , " + stateString;
                        iState = iState / deepLayers[k].stateSize;
                    }
                }
                Console.WriteLine(stateString + " : " + Translator.symbolToText(output.ToArray()));
            }
        }

        public static void copyInto(Couppy original, Couppy destination)
        {
            RestrictedStatePredictor.copyInto(original.inputLayer, destination.inputLayer);
            if (original.deepLayers != null)
            {
                for (int i = 0; i < original.deepLayers.Length; i++)
                    StatePredictor.copyInto(original.deepLayers[i], destination.deepLayers[i]);
            }
            else
                destination.deepLayers = null;

            original.layerSizes.CopyTo(destination.layerSizes, 0);
            destination.name = original.name;
            destination.trainingDepth = original.trainingDepth;
        }

        public static void leapOverride(Couppy original, Couppy destination, Random rdm, int intensity)
        {
            //Copy original to destination
            Couppy.copyInto(original, destination);
            destination.inputLayer.reset();

            destination.trainingDepth = original.trainingDepth + 1;
            destination.inputLayer.trainingDepth = original.trainingDepth + 1;
            if(original.deepLayers != null)
                for (int i = 0; i < destination.deepLayers.Length; i++)
                    destination.deepLayers[i].trainingDepth = original.deepLayers[i].trainingDepth + 1;

            int changeCount = intensity;

            //Make changes
            int layer;
            int index;
            StatePredictor layerPredictor;
            int dnaLength = original.getDnaLength();
            for (int i = 0; i < changeCount; i++)
            {
                index = rdm.Next(dnaLength);

                layer = -1;
                if (index < original.inputLayer.stateTransitionTable.Length)
                    layer = 0;
                else
                {
                    index -= original.inputLayer.stateTransitionTable.Length;
                    for (int k = 0; k < original.deepLayers.Length; k++)
                    {
                        if (index < original.deepLayers[k].table.Length)
                        {
                            layer = k + 1;
                        }
                        else
                        {
                            index -= original.deepLayers[k].table.Length;
                        }
                    }
                }

                if(layer == 0)
                {
                    destination.inputLayer.stateTransitionTable[index] = (byte)rdm.Next(destination.inputLayer.stateSize); //New random state at index
                }
                else
                {
                    //Choose random table index for change
                    layerPredictor = destination.deepLayers[layer - 1];

                    //Check if index is prediction image or state image
                    if (index % 2 == 0)
                        layerPredictor.table[index] = (byte)rdm.Next(layerPredictor.symbolSize); //New random prediction at index
                    else
                        layerPredictor.table[index] = (byte)rdm.Next(layerPredictor.stateSize); //New random state at index
                }
            }
        }

        public class Trainer
        {
            Couppy target;

            public bool training = false;

            Couppy best;
            object checkLock;
            List<Thread> threads;
            List<object> readLocks;
            List<object> trainLocks;
            List<Couppy> chatBots;

            SymbolCorpus trainSet;


            int[] trainIntensity;
            int[] trainSampleCount;
            double[] trainAccumulatedAccuracies;


            int dnaLength;

            public Trainer(Couppy target)
            {
                this.target = target;
                checkLock = new object();
                threads = new List<Thread>();
                readLocks = new List<object>();
                trainLocks = new List<object>();
                chatBots = new List<Couppy>();

                dnaLength = target.getDnaLength();
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

                best = new Couppy(target.layerSizes);
                Couppy.copyInto(target, best);
                best.inputLayer.reset();
                best.bake(trainSet);
                TrainingUpdated?.Invoke("Layer training started with: " + best.inputLayer.getStats());

                for (int i = 0; i < trainSampleCount.Length; i++)
                {
                    trainSampleCount[i] = 1;
                    trainAccumulatedAccuracies[i] = best.inputLayer.accuracy;
                }


                for (int i = 0; i < threadCount; i++)
                {
                    //Add thread lock
                    readLocks.Add(new object());
                    trainLocks.Add(new object());
                    //Create thread resource
                    Couppy tester = new Couppy(best.layerSizes);
                    chatBots.Add(tester);
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
                        Couppy.leapOverride(best, chatBots[index], rdm, usedIntensity);
                    }

                    chatBots[index].bake(trainSet);

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

                        if (chatBots[index].inputLayer.accuracy > best.inputLayer.accuracy)
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
                                    trainAccumulatedAccuracies[i] += chatBots[index].inputLayer.accuracy - best.inputLayer.accuracy;
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

                            //if (chatBots[index].trainingDepth % 10 == 0)
                            //    printTrainingIntensityStatus();

                            //Start copy procedure
                            //Lock all read locks
                            for (int i = 0; i < readLocks.Count; i++)
                            {
                                Monitor.Enter(readLocks[i]);
                            }

                            //Copy into best
                            Couppy.copyInto(chatBots[index], best);

                            TrainingUpdated?.Invoke(("Leap by thread " + index + " : " + best.inputLayer.getStats()));
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
                Couppy.copyInto(best, target);
            }
        }

        public delegate void TrainingUpdateHandler(string message);
        public static event TrainingUpdateHandler TrainingUpdated;
    }
}
