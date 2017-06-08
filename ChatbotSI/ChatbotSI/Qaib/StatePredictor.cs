using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatbotSI
{
    [Serializable()]
    public class StatePredictor
    {
        public int trainingDepth;

        public int inputSymbolSize;
        public int outputSymbolSize;
        public int stateSize;

        public byte[] table;
        public short tableStride;

        public byte state;


        public int iterationCount;
        public int[] stateMetrics;
        public float stateEntropy
        {
            get
            {
                float information = 0;
                double prob;
                for (int i = 0; i < stateMetrics.Length; i++)
                {
                    prob = stateMetrics[i] / (double)iterationCount;
                    if (prob > 0)
                        information -= (float)(prob * Math.Log(prob, stateSize));
                }
                return information;
            }
        }

        public StatePredictor()
        {

        }
        public StatePredictor(int inputSymbolSize, int outputSymbolSize, int stateSize)
        {
            this.inputSymbolSize = inputSymbolSize;
            this.outputSymbolSize = outputSymbolSize;
            this.stateSize = stateSize;

            //Create table with input(inputSymbolSize , stateSize) each with 2 entries ( output , newState )
            table = new byte[inputSymbolSize * stateSize * 2];
            //Stride, bytes per symbol (states + (prediction , newState))
            tableStride = (short)(stateSize * 2); // stateSize times 2 output bytes
            stateMetrics = new int[stateSize];
            //i0 , s0: p00 , s00
            //i0 , s1: p01 , s01
            //...
        }

        public void reset()
        {
            iterationCount = 0;
            state = 0;
            for (int i = 0; i < stateMetrics.Length; i++)
                stateMetrics[i] = 0;
        }

        //Make a single iteration and update state
        public byte process(byte input)
        {
            //Increment registered iterations
            iterationCount++;

            //Increment one prediction made with current state
            stateMetrics[state]++;

            //Object index
            int index = (input * tableStride + state * 2);

            //Change state
            state = table[index + 1];

            //Return output
            return table[index + 0];
        }

        public static void copyInto(StatePredictor original, StatePredictor destination)
        {
            original.table.CopyTo(destination.table, 0);
            destination.iterationCount = original.iterationCount;
            destination.state = original.state;
            destination.stateSize = original.stateSize;
            destination.inputSymbolSize = original.inputSymbolSize;
            destination.tableStride = original.tableStride;
            destination.trainingDepth = original.trainingDepth;
            original.stateMetrics.CopyTo(destination.stateMetrics, 0);
        }
    }

}
