using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RandomGaussian
{
    /// <summary>
    /// Sample from a Gaussian distribution, with a given mean and standard deviation
    /// </summary>
    /// <param name="mean">Mean of the gaussian distribution</param>
    /// <param name="std">Standard deviation of the gaussian distribution</param>
    /// <param name="extrema">How extreme a value can be (computed as number of standard deviations</param>
    public static float Sample(float mean, float std, float extrema)
    {
        float output;

        do
        {
            output = NextGaussian();
        } while (Mathf.Abs(output) > extrema);

        output = mean + output * std;
        return output;
    }

    /// <summary>
    /// Sample from a Gaussian distribution using the Marsaglia polar method
    /// https://www.alanzucconi.com/2015/09/16/how-to-sample-from-a-gaussian-distribution/
    /// </summary>
    /// <returns></returns>
    private static float NextGaussian()
    {
        float v1, v2, s;
        do
        {
            v1 = 2.0f * Random.Range(0f, 1f) - 1.0f;
            v2 = 2.0f * Random.Range(0f, 1f) - 1.0f;
            s = v1 * v1 + v2 * v2;
        } while (s >= 1.0f || s == 0f);

        s = Mathf.Sqrt((-2.0f * Mathf.Log(s)) / s);

        return v1 * s;
    }
}
