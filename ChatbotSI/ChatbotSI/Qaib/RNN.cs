using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatbotSI
{
    class RNN
    {
        int streamCount;
        int neuronCount;
        int stateCount;

        float[][] predictionWeights;
        float[][] stateWeights;

        public RNN(int inputSize, int hiddenNeurons, int states)
        {
            streamCount = inputSize;
            neuronCount = hiddenNeurons;
            stateCount = states;
        }

        RNNOutput process(RNNInput input)
        {
            throw new Exception();
        }

        public RNNProcessResult process(float[][] input)
        {
            throw new Exception();
            //RNNInput bakedInput = new RNNInput();
            //bakedInput.input;
        }

        public RNNProcessResult train(float[][] input)
        {
            throw new Exception();
        }
        struct RNNLayer
        {
            int inputSize;
            int neuronCount;

            float[][] weights;

            public RNNLayer(int inputSize, int neuronCount)
            {
                this.neuronCount = neuronCount;
                this.inputSize = inputSize;

                Random rdm = new Random();

                weights = new float[neuronCount][];
                for (int i = 0; i < weights.Length; i++)
                {
                    weights[i] = new float[inputSize];
                    for (int j = 0; j < weights[i].Length; j++)
                    {
                        weights[i][j] = 2f * (float)rdm.NextDouble() - 1f;
                    }
                }
            }

            public float[] process(float[] input)
            {
                float[] output = new float[neuronCount];

                for(int i=0;i<neuronCount;i++)
                {
                    output[i] = 0;
                    for(int j=0;j<inputSize;j++)
                    {
                        output[i] += input[j] * weights[i][j];
                    }
                    output[i] = (float)(Math.Atan(output[i])/(Math.PI) + 0.5f);
                }

                return output;
            }
        }

        public struct RNNInput
        {
            public float[] input;
            public float[] state;
        }

        public struct RNNOutput
        {
            public float[] prediction;
            public float[] state;
        }

        public struct RNNProcessResult
        {
            byte[] predictions;
            //byte[] states;

            float error;
        }
    }
}
