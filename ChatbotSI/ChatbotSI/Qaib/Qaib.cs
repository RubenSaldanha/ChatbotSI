using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;

namespace ChatbotSI
{

    class Qaib
    {
        int populationCount;
        int permanence;
        int children;
        List<TablePredictor> population;

        int cycle;

        byte[] test;
        
        public Qaib(MainWindow window)
        {

            //Load test bytes
            test = load();

            //truncate test bytes
            byte[] truncatedTest = new byte[256];
            for (int i = 0; i < truncatedTest.Length; i++)
                truncatedTest[i] = test[i];
            test = truncatedTest;

            test = CharToSymbol(test);

            basicPredictTablePrint(test);

            //print Test bytes metrics
            Console.WriteLine("Test text loaded with: " + test.Length + " Bytes.");
            Console.WriteLine("Test text loaded with: " + test.Length/1024f + " KBytes.");
            Console.WriteLine("Test text loaded with: " + (test.Length / 1024f)/1014f + " MBytes.");

            //Set char alphabet as test for translation display
            //test = new byte[256];
            //for (int i = 0; i < 256; i++)
            //    test[i] = (byte)i;

            bool printTestStrings = false;
            if (printTestStrings)
            {
                string inputText = "";
                for (int i = 0; i < test.Length; i++)
                    inputText += (char)test[i];
                Console.WriteLine("\nTest string:");
                Console.WriteLine(inputText);

                test = CharToSymbol(test);
                string symbolBytes = "";
                for (int i = 0; i < test.Length; i++)
                    symbolBytes += ("" + (int)test[i]).PadLeft(4);
                Console.WriteLine("\nTest symbol bytes:");
                Console.WriteLine(symbolBytes);

                test = SymbolToChar(test);
                string symbolText = "";
                for (int i = 0; i < test.Length; i++)
                    symbolText += (char)test[i];
                Console.WriteLine("\nTest symbol texted:");
                Console.WriteLine(symbolText);
            }

            //test = load();
            //test = CharToSymbol(test);

            TablePredictor f0 = TablePredictor.LoadFromFile("AAABakef0.tp");
            byte[] g0 = f0.predictStates(test);
            Console.WriteLine(f0.getStats());
            test = g0;
            symbolCount = 64;

            //more layers
            TablePredictor f1 = TablePredictor.LoadFromFile("aaap.tp");
            byte[] g1 = f1.predictStates(g0);
            Console.WriteLine(f1.getStats());

            //TablePredictor f2 = TablePredictor.LoadFromFile("Bakef2.tp");
            //byte[] g2 = f2.predictStates(g1);
            //Console.WriteLine(f2.getStats());

            //TablePredictor f3 = TablePredictor.LoadFromFile("Bakef3.tp");
            //byte[] g3 = f3.predictStates(g2);
            //Console.WriteLine(f3.getStats());

            byte[] display;
            //display = f3.project(g3);
            //display = f2.project(g2);
            display = f1.project(g1);
            display = f0.project(display);
            display = SymbolToChar(display);
            string os = "";
            for (int i = 0; i < display.Length; i++)
                os += (char)display[i];
            Console.WriteLine("\nTest symbol texted:");
            Console.WriteLine(os);

            //Setup2();
        }

        void basicPredictTablePrint(byte[] input)
        {
            LoadC2S();

            int[][] table = new int[symbolCount][];
            for (int i = 0; i < table.Length; i++)
                table[i] = new int[symbolCount];

            for(int i=1;i<input.Length;i++)
            {
                table[input[i - 1]][input[i]]++;
            }

            byte[] predictor = new byte[symbolCount];

            for(int i=0;i<symbolCount;i++)
            {
                int bestValue = int.MinValue;
                int bestIndex = -1;
                for(int j=0;j<symbolCount;j++)
                {
                    if(table[i][j] > bestValue)
                    {
                        bestValue = table[i][j];
                        bestIndex = j;
                    }
                }

                predictor[i] = (byte)bestIndex;
            }

            int[] errors = new int[symbolCount];
            int[] instances = new int[symbolCount];
            int errorCount = 0;
            for (int i=1;i<input.Length;i++)
            {
                instances[input[i - 1]]++;

                if (predictor[input[i - 1]] != input[i])
                {
                    errorCount++;
                    errors[input[i - 1]]++;
                }
            }

            Console.WriteLine("Basic Table accuracy: " + (1f - errorCount / (float)(input.Length - 1)));

            for(int i=0;i<symbolCount;i++)
            {
                Console.WriteLine("" + (char)SymbolToChar((byte)i) + " : " + ("" + (1f - errors[i] / (float)instances[i])).PadRight(10).Substring(0,4) + " with : " + (char)SymbolToChar(predictor[i]));
            }
        }

        void Setup2()
        {
            populationCount = 1;
            cycle = 0;

            population = new List<TablePredictor>();

            //LoadC2S();

            Random rdm = new Random(0);
            for (int i = 0; i < populationCount; i++)
            {
                population.Add(new TablePredictor(symbolCount, 64, rdm.Next()));
                population[i].testPredict(test);
            }


            Thread tt = new Thread(VFLearner);
            tt.Start();
        }
        void VFLearner()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            Random rdm = new Random(0);

            TablePredictor testTable = new TablePredictor(symbolCount, population[0].stateSize);
            TablePredictor temp;

            int printRun = 100000;

            double crossRatio = 0.0;
            double randomRatio = 0.1;
            double mutatedRatio = 1 - (crossRatio + randomRatio);
            double mutationIntensity = 0.5;
            int runs = int.MaxValue;
            double decision;
            int parent;
            for(int i=0;i<runs;i++)
            {
                cycle++;

                if(i% printRun == 0)
                {
                    population[0].SaveToFile("Run0-" + (i / printRun) + "v0.tp");
                    Console.WriteLine("m.i.: " + ("" + mutationIntensity).PadRight(10).Substring(0, 6));
                    printPopulation(1);
                }

                decision = rdm.NextDouble();
                if (decision < mutatedRatio)
                {
                    //Mutation
                    parent = rdm.Next(populationCount);
                    TablePredictor.mutateOverride(testTable, population[parent], rdm, mutationIntensity);
                    testTable.birthTime = cycle;
                }
                else if(decision < mutatedRatio + crossRatio)
                {
                    throw new Exception("NO CROSS ALLOWED");
                    //Cross
                    int parent1 = rdm.Next(populationCount);
                    int parent2 = rdm.Next(populationCount);
                    TablePredictor.crossOverride(testTable, population[parent1], population[parent2], rdm);
                    testTable.birthTime = cycle;

                    if (testTable.species == population[parent1].species)
                        parent = parent1;
                    else
                        parent = parent2;
                }
                else
                {
                    TablePredictor.randomOverride(testTable, rdm);
                    parent = -1;
                }

                testTable.testPredict(test);

                if(testTable.accuracy > population[populationCount - 1].accuracy)
                {
                    if(parent != -1)
                    {
                        //Children analysis
                        if(population[parent].accuracy < testTable.accuracy)
                        {
                            //Parent beat
                            //Soft change for sucessful mutation intensity (x2 because of random average behaviour)
                            mutationIntensity = 0.8 * mutationIntensity + 0.2 * (2 * testTable.mutationIntensity);
                            if (mutationIntensity > 0.9)
                                mutationIntensity = 0.9; //maximum allowed mutation intensity
                            temp = population[parent];
                            population[parent] = testTable;
                            testTable = temp;

                            //Reorder
                            for(int j=parent;j>0;j--)
                            {
                                if(population[j-1].accuracy < population[j].accuracy)
                                {
                                    temp = population[j - 1];
                                    population[j - 1] = population[j];
                                    population[j] = temp;
                                }
                            }
                        }
                    }
                    else
                    {
                        //Random win, order
                        for(int j=0;j<populationCount;j++)
                        {
                            if(testTable.accuracy > population[j].accuracy)
                            {
                                temp = population[j];
                                population[j] = testTable;
                                testTable = temp;
                            }
                        }
                    }
                }
            }

            stopwatch.Stop();
            long elapsedTime = stopwatch.ElapsedMilliseconds;
            Console.WriteLine("Genetic Algorithms runtime at: \nMiliseconds: " + elapsedTime + "\nSeconds: " + elapsedTime / 1000f + "\nMinutes" + (elapsedTime / 1000f) / 60f);

            printPopulation(populationCount);

            TablePredictor best = population[0];
            Console.WriteLine("Rank : 0 " + best.getStats());


            best.SaveToFile("Potato.tp");

            TablePredictor lel = TablePredictor.LoadFromFile("Potato.tp");
            lel.testPredict(test);
            Console.WriteLine("Loaded: " + lel.getStats());
        }
        void Setup1()
        {
            populationCount = 14;
            permanence = 10;
            children = 4;

            population = new List<TablePredictor>();

            Random rdm = new Random(0);
            for (int i = 0; i < populationCount; i++)
            {
                population.Add(new TablePredictor(symbolCount, 128, rdm.Next()));
            }

            cycle = 0;

            for (int i = 0; i < population.Count; i++)
            {
                population[i].testPredict(test);
            }


            Thread tt = new Thread(Learner);
            tt.Start();
        }
        void Learner()
        {
            //Start Genetic Algorithms
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            for (int i = 0; i < 100000; i++)
            {
                Iterate();
                if(i%100 == 0)
                printPopulation(1);
            }

            Console.WriteLine("Learner over, best result at: ");
            printPopulation(int.MaxValue);

            stopwatch.Stop();
            long elapsedTime = stopwatch.ElapsedMilliseconds;
            Console.WriteLine("Genetic Algorithms runtime at: \nMiliseconds: " + elapsedTime + "\nSeconds: " + elapsedTime / 1000f + "\nMinutes" + (elapsedTime / 1000f) / 60f);

            TablePredictor best = population[0];
            Console.WriteLine("Rank : 0 " + best.getStats());
            population.Clear();


            Console.WriteLine("Reforcer Start: ");
            stopwatch = new Stopwatch();
            stopwatch.Start();

            Reforcer(best);


            stopwatch.Stop();
            elapsedTime = stopwatch.ElapsedMilliseconds;
            Console.WriteLine("Reforcer runtime at: \nMiliseconds: " + elapsedTime + "\nSeconds: " + elapsedTime / 1000f + "\nMinutes" + (elapsedTime / 1000f) / 60f);

            Console.WriteLine("Best \n" + best.getStats());
        }
        void Iterate()
        {
            Random rdm = new Random();

            //List<float> crossPerformance = new List<float>();
            //List<float> mutationPerformance = new List<float>(); ;
            //List<float> randomPerformance = new List<float>(); ;

            //Create mutated children
            double crossRatio = 0.5;
            for(int i=permanence;i<permanence + children;i++)
            {
                if (rdm.NextDouble() < crossRatio)
                {
                    //Cross
                    int parent1 = rdm.Next(permanence);
                    int parent2 = rdm.Next(permanence);
                    TablePredictor.crossOverride(population[i], population[parent1], population[parent2], rdm);
                    population[i].birthTime = cycle;

                    population[i].testPredict(test);

                    //crossPerformance.Add(population[i].accuracy);
                }
                else
                {
                    //Mutation
                    int parent = rdm.Next(permanence);
                    TablePredictor.mutateOverride(population[i], population[parent], rdm, 0.3);
                    population[i].birthTime = cycle;

                    population[i].testPredict(test);

                    //mutationPerformance.Add(population[i].accuracy);
                }
            }

            //Add randoms
            for (int i = permanence + children; i < population.Count; i++)
            {
                TablePredictor.randomOverride(population[i], rdm);
                population[i].testPredict(test);
                //randomPerformance.Add(population[i].accuracy);
            }

            //sort to find weaker individuals
            sortPopulation();

            //Convert weaker same species individuals to randoms
            for(int i=0;i<population.Count;i++)
            {
                for (int j = population.Count - 1; j > i; j--)
                {
                    if (population[j].species == population[i].species)
                    {
                        TablePredictor.randomOverride(population[j], rdm);
                        population[j].testPredict(test);
                        //randomPerformance.Add(population[j].accuracy);
                    }
                }
            }

            //sort again
            sortPopulation();

            //mutation measures
            //float waterlineAccuracy = population[permanence-1].accuracy;

            //int pCount = 0;
            //for (int i = 0; i < mutationPerformance.Count; i++)
            //    if (mutationPerformance[i] > waterlineAccuracy)
            //        pCount++;

            //if(mutationPerformance.Count > 0)
            //    Console.WriteLine("Mutation Performance: " + pCount / (float)mutationPerformance.Count);

            //pCount = 0;
            //for (int i = 0; i < crossPerformance.Count; i++)
            //    if (crossPerformance[i] > waterlineAccuracy)
            //        pCount++;

            //if (crossPerformance.Count > 0)
            //    Console.WriteLine("Cross Performance: " + pCount / (float)crossPerformance.Count);

            //pCount = 0;
            //for (int i = 0; i < randomPerformance.Count; i++)
            //    if (randomPerformance[i] > waterlineAccuracy)
            //        pCount++;

            //if (randomPerformance.Count > 0)
            //    Console.WriteLine("Random Performance: " + pCount / (float)randomPerformance.Count);

            cycle++;
        }
        void sortPopulation()
        {
            TablePredictor temp;
            for(int i=0;i<populationCount;i++)
            {
                for(int j=0;j<populationCount-1;j++)
                {
                    if(population[j].accuracy < population[j+1].accuracy)
                    {
                        temp = population[j];
                        population[j] = population[j + 1];
                        population[j + 1] = temp;
                    }
                }
            }
        }
        void printPopulation(int length)
        {
            Console.WriteLine("Cycle: " + cycle);

            int printLength = (population.Count < length) ? population.Count : length;
            for (int i=0;i<printLength ;i++) //i<pop.count
            {
                Console.WriteLine((i + "").PadRight(20).Substring(0,6) + " :: s. " + (population[i].species + "").PadRight(20).Substring(0, 10) + " :: sd. " + (population[i].speciesDepth + "").PadRight(20).Substring(0, 6) + " :: acc.: " + population[i].accuracy + " :: origin: " + population[i].origin + " :: s.entropy: " + ("" + population[i].stateEntropy).PadRight(10).Substring(0,4));
            }
        }

        void Reforcer(TablePredictor table)
        {
            Random rdm = new Random();

            TablePredictor tester = new TablePredictor(table);
            int breath = 256;

            for(int i=0;i<2;i++)
            {
                if(Reforce(table, tester, rdm, breath))
                    Console.WriteLine(i + " Reforce result: " + table.accuracy);
                else
                    Console.WriteLine(i + " Reforce failed. ");
            }
        }
        bool Reforce(TablePredictor original, TablePredictor factoryTester, Random rdm, int breath)
        {
            int variationCount = ((int)short.MaxValue) * 2 * 256; //All entries, times 2 (prediction,state) , times 256 possible states
            int startVariation = rdm.Next(variationCount);
            int count = 0;
            for(int i=startVariation;i< variationCount;i = (i+1)%variationCount)
            {
                count++;
                if (count > breath)
                    return false;

                TablePredictor.variationOverride(factoryTester, i);

                factoryTester.ResetState();
                factoryTester.ResetMetrics();
                factoryTester.testPredict(test);
                
                if(original.accuracy < factoryTester.accuracy)
                {
                    TablePredictor.variationOverride(original, i);
                    original.predictionCount = factoryTester.predictionCount;
                    original.errorCount = factoryTester.errorCount;
                    return true; //return both the original and the factory tester with the same variation
                }

                TablePredictor.variationUndo(factoryTester, i, original);
            }

            return false;
        }

        byte[] load()
        {
            string[] lines = System.IO.File.ReadAllLines("test.txt");
            List<byte> info = new List<byte>();

            int conversationCount = 0;
            int conversationLength = 0;
            for (int i = 1; i < lines.Length; i++)
            {
                if(lines[i] == "")
                {
                    //Add return to separate conversations
                    info.Add((byte)'\r');
                    info.Add((byte)'\n');

                    conversationCount++;
                    conversationLength = 0;

                    i++; //skip "Conversation"
                    continue;
                }
                else
                {
                    conversationLength++;
                    for (int j = 0; j < lines[i].Length; j++)
                        info.Add((byte)lines[i][j]);

                    //Add returns
                    info.Add((byte)'\r');
                    info.Add((byte)'\n');
                }
            }

            Console.WriteLine("Conversations Loaded: " + conversationCount);
            return info.ToArray();
        }

        //Consider using a byte lookup array(matrix)
        private int iDist(short a, short b)
        {
            int count = 0;
            for(int i=0; i < 16;i++)
            {
                if (a % 2 != b % 2)
                    count++;

                a /= 2;
                b /= 2;
            }

            return count;
        }

        public class TablePredictor
        {
            public enum OriginType { Random, Mutation, Cross }
            public OriginType origin;
            public int mutationLeap;
            public double mutationIntensity
            {
                get { return mutationLeap / (double)table.Length;  }
            }

            public static int speciesCounter = 0;
            public int species;
            public int speciesDepth;
            public int birthTime;

            int symbolSize;
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
                        if(prob > 0)
                            information -= (float)(prob * Math.Log(prob, 2));
                    }
                    return information;
                }
            }

            public float accuracy
            {
                get { return 1f - errorCount / (float)predictionCount; }
            }

            public TablePredictor(int symbolSize, int stateSize)
            {
                this.symbolSize = symbolSize;
                this.stateSize = stateSize;
                Initialize();
            }
            public TablePredictor(int symbolSize, int stateSize , int seed)
            {
                this.symbolSize = symbolSize;
                this.stateSize = stateSize;
                Initialize();

                randomOverride(this, new Random(seed));
                //Random rdm = new Random(seed);
                //rdm.NextBytes(table);

                //speciesCounter++;
                //species = speciesCounter;
                //speciesDepth = 0;
            }
            public TablePredictor(TablePredictor copy)
            {
                this.symbolSize = copy.symbolSize;
                this.stateSize = copy.stateSize;
                Initialize();

                copy.table.CopyTo(table, 0);
                state = copy.state;
                species = copy.species;
                speciesDepth = copy.speciesDepth;
                birthTime = copy.birthTime;
            }

            void Initialize()
            {
                //Create table with 2^8 (input) * 2^8 (state), x2 entries , with , ( prediction , newState )
                table = new byte[symbolSize*stateSize * 2];
                //Stride, bytes per symbol (states + (prediction , newState))
                tableStride = (short)(stateSize * 2); // stateSize times 2 output bytes
                stateMetrics = new int[stateSize];
                //i0 , s0:
                //i0 , s1:
                //...
                //i255 , s255:
            }
            public void FactoryReset()
            {
                state = 0;
                ResetMetrics();
                species = -1;
                speciesDepth = -1;
                birthTime = -1;
            }
            public void ResetMetrics()
            {
                predictionCount = 0;
                errorCount = 0;
                for (int i = 0; i < stateMetrics.Length;i++)
                    stateMetrics[i] = 0;
            }
            public void ResetState()
            {
                state = 0;
            }

            public byte predict(byte input)
            {
                stateMetrics[state]++;
                int index = (input * tableStride + state * 2);
                state = table[index + 1];
                return table[index + 0];
            }
            public byte[] predict(byte[] input)
            {
                byte[] predictions = new byte[input.Length];

                int errors = 0;

                //Make first prediction, it doesn't have input (default 0)
                state = 0;
                predictions[0] = predict(0);
                if (predictions[0] != input[0])
                    errorCount++;

                //Make the rest of the predictions
                for(int i=1;i<input.Length;i++)
                {
                    predictions[i] = predict(input[i-1]);

                    if (predictions[i] != input[i])
                        errors++;
                }

                errorCount += errors;
                predictionCount += predictions.Length;
                return predictions;
            }
            public byte[] predictStates(byte[] input)
            {
                byte[] states = new byte[input.Length];
                int errors = 0;

                byte prediction;
                //Make first prediction, it doesn't have input (default 0)
                state = 0;
                prediction = predict(0);
                states[0] = state;
                if (prediction != input[0])
                    errorCount++;

                //Make the rest of the predictions
                for (int i = 1; i < input.Length; i++)
                {
                    prediction = predict(input[i - 1]);
                    states[i] = state;

                    if (prediction != input[i])
                        errors++;
                }

                errorCount += errors;
                predictionCount += input.Length;

                return states;
            }
            public void testPredict(byte[] input)
            {
                int errors = 0;

                byte prediction;
                //Make first prediction, it doesn't have input (default 0)
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

            public byte[] project(byte[] control)
            {
                byte[] output = new byte[control.Length];

                state = 0;
                output[0] = predict(0);

                for(int i=1;i<output.Length;i++)
                {
                    state = control[i - 1];
                    output[i] = predict(output[i - 1]);
                }

                return output;
            }

            public string getStats()
            {
                string stats = "";

                stats += "Species : " + species + "\n";
                stats += "Species Depth: " + speciesDepth + "\n";
                stats += "Symbol size: " + symbolSize + "\n";
                stats += "State size: " + stateSize + "\n";
                stats += "Prediction Count: " + predictionCount + "\n";
                stats += "Accuracy: " + accuracy + "\n";
                stats += "State entropy: " + stateEntropy + "\n";

                return stats;
            }

            public void SaveToFile(string file)
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
                bR = BitConverter.GetBytes(speciesDepth);
                bR.CopyTo(saveArray, 9);

                //Table
                table.CopyTo(saveArray, 13);
                System.IO.File.WriteAllBytes(file, saveArray);
            }
            public static TablePredictor LoadFromFile(string file)
            {
                byte[] saveArray = System.IO.File.ReadAllBytes(file);

                TablePredictor load = null;

                //version check
                if (saveArray[0] == 0)
                {
                    int symbolSize = BitConverter.ToInt32(saveArray, 1);
                    int stateSize = BitConverter.ToInt32(saveArray, 5);
                    load = new TablePredictor(symbolSize, stateSize);
                    load.speciesDepth = BitConverter.ToInt32(saveArray, 9);
                    for (int i = 0; i < load.table.Length; i++)
                        load.table[i] = saveArray[13 + i];
                }
                else
                {
                    throw new Exception("Invalid file version.");
                }

                return load;
            }

            public static TablePredictor mutate(TablePredictor original, Random rdm, double intensity)
            {
                TablePredictor mutated = new TablePredictor(original.symbolSize, original.stateSize);

                mutateOverride(mutated, original, rdm, intensity);

                return mutated;
            }
            public static void mutateOverride(TablePredictor destination, TablePredictor original, Random rdm, double intensity)
            {
                original.table.CopyTo(destination.table,0);
                destination.FactoryReset();

                destination.species = original.species;
                destination.speciesDepth = original.speciesDepth + 1;

                //not very optimal
                int mutationCount = rdm.Next(2 + (int)(original.table.Length * intensity));
                destination.mutationLeap = mutationCount;
                //int mutationCount = rdm.Next((int)(1 + original.table.Length * 0.3f));


                int index;
                for (int i = 0; i < mutationCount; i++)
                {
                    index = rdm.Next(destination.table.Length);

                    if (index%2 == 0)
                        destination.table[index] = (byte)rdm.Next(destination.symbolSize);
                    else
                        destination.table[index] = (byte)rdm.Next(destination.stateSize);

                    //inputIndex = rdm.Next(original.symbolSize);
                    //stateIndex = rdm.Next(original.stateSize);
                    //outputChange = (rdm.NextDouble() < 0.5f) ? 1 : 0;

                    //Optimize this, for %2 check
                    //if(outputChange == 0)
                    //    destination.table[inputIndex * original.tableStride + stateIndex * 2 + outputChange] = (byte)rdm.Next(destination.symbolSize);
                    //else
                    //    destination.table[inputIndex * original.tableStride + stateIndex * 2 + outputChange] = (byte)rdm.Next(destination.stateSize);
                }

                destination.origin = OriginType.Mutation;
            }
            public static TablePredictor cross(TablePredictor parent1, TablePredictor parent2, Random rdm)
            {
                TablePredictor mutated = new TablePredictor(parent1.symbolSize, parent1.stateSize);

                crossOverride(mutated, parent1, parent2, rdm);

                return mutated;
            }
            public static void crossOverride(TablePredictor destination, TablePredictor parent1, TablePredictor parent2, Random rdm)
            {
                parent1.table.CopyTo(destination.table, 0);
                destination.FactoryReset();

                //not very optimal
                int parent2Count = rdm.Next(destination.table.Length);

                destination.species = (parent2Count > destination.table.Length / 2) ? parent2.species : parent1.species;
                destination.speciesDepth = (parent1.speciesDepth > parent2.speciesDepth) ? parent1.speciesDepth : parent2.speciesDepth;

                int index;
                for (int i = 0; i < parent2Count; i++)
                {
                    index = rdm.Next(destination.table.Length);
                    destination.table[index] = parent2.table[index];
                }

                destination.origin = OriginType.Cross;
            }
            public static void randomOverride(TablePredictor destination, Random rdm)
            {
                destination.FactoryReset();
                speciesCounter++;
                destination.species = speciesCounter;
                destination.speciesDepth = 0;

                
                for(int i=0;i<destination.table.Length;i++)
                {
                    //Check if slot is prediction output, or new state output
                    if (i % 2 == 0)
                        destination.table[i] = (byte)rdm.Next(destination.symbolSize);
                    else
                        destination.table[i] = (byte)rdm.Next(destination.stateSize);
                }

                destination.origin = OriginType.Random;
            }
            public static void variationOverride(TablePredictor destination, int variation)
            {
                throw new Exception();
                int index = variation / (256 * 2);
                int field = (variation / 256) % 2;
                int mutation = variation % 256;

                destination.table[index * 2 + field] = (byte)mutation;
            }
            public static void variationUndo(TablePredictor destination, int variation, TablePredictor original)
            {
                throw new Exception();
                int index = variation / (256 * 2);
                int field = (variation / 256) % 2;
                int mutation = variation % 256;

                destination.table[index * 2 + field] = original.table[index*2 + field];
            }
        }

        static int symbolCount;
        static byte[] c2s;
        static byte[] s2c;
        public static byte CharToSymbol(byte input)
        {
            if (c2s == null)
                LoadC2S();

            return c2s[input];
        }
        public static byte[] CharToSymbol(byte[] input)
        {
            byte[] symbols = new byte[input.Length];
            for(int i=0;i<input.Length;i++)
            {
                symbols[i] = CharToSymbol(input[i]);
            }
            return symbols;
        }
        public static byte SymbolToChar(byte symbol)
        {
            if (s2c == null)
                LoadC2S();

            return s2c[symbol];
        }
        public static byte[] SymbolToChar(byte[] symbols)
        {
            byte[] output = new byte[symbols.Length];
            for (int i = 0; i < symbols.Length; i++)
            {
                output[i] = SymbolToChar(symbols[i]);
            }
            return output;
        }
        static void LoadC2S()
        {
            List<byte> symbols = new List<byte>();

            symbols.Add((byte)'\n');
            symbols.Add((byte)' ');
            symbols.Add((byte)'.');
            symbols.Add((byte)',');
            symbols.Add((byte)'!');
            symbols.Add((byte)'?');

            //add numbers
            for(int i=0;i<10;i++)
                symbols.Add((byte)((byte)'0' + (byte)i));

            //add lower-case letters
            for(int i=0;i<26;i++)
                symbols.Add((byte)((byte)'a' + (byte)i));

            //add upper-case letters
            //for (int i = 0; i < 26; i++)
            //    symbols.Add((byte)((byte)'A' + (byte)i));

            //populate symbol to char table
            s2c = new byte[256];
            for(int i=0;i<256;i++)
            {
                if (i < symbols.Count)
                    s2c[i] = symbols[i];
                else
                    s2c[i] = (byte)'#';
            }

            //populate char to symbol table
            c2s = new byte[256];
            for(int i=0;i<256;i++)
            {
                //default case 
                byte translation = (byte)symbols.Count;

                //find translation for i
                for (int k = 0; k < symbols.Count; k++)
                    if (i == (int)symbols[k])
                        translation = (byte)k; //translates to order in list

                //set table
                c2s[i] = translation;
            }

            //Upper case char to symbol
            //Find lower a symbol index
            int aIndex = 0;
            for (int i = 0; i < symbols.Count; i++)
                if (symbols[i] == (byte)'a')
                    aIndex = i;

            //set upper case char to lower case symbol
            for (int i = 0; i < 26; i++)
                c2s[((byte)'A') + i] = (byte)(aIndex + i);

            //Add one more theorical symbol, the 'default', which translates from 'not in table' to 'lastCount' , and from 'lastCount' to '#'
            symbolCount = symbols.Count + 1;
            Console.WriteLine("Symbol library created with " + symbolCount + " symbols");
        }
    }
}
