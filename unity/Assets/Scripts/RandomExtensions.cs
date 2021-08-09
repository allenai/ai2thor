// Some extension methods for System.Random for creating a few more kinds of random stuff.
using System;
using System.Collections;
using System.Collections.Generic;

namespace RandomExtensions {
    public static class RandomExtensions {
        /**
         * Generates normally distributed numbers. Each operation makes two Gaussians for the price of one,
         * and apparently they can be cached or something for better performance, but who cares.
         *
         * @param mu Mean of the distribution
         * @param sigma Standard deviation
         */
        public static double NextGaussian(this Random r, double mu = 0, double sigma = 1) {
            var u1 = r.NextDouble();
            var u2 = r.NextDouble();
            var rand_std_normal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            var rand_normal = mu + sigma * rand_std_normal;

            // Very rarely, it is possible to have underflow or an infinity if u1/u1 = 0.
            // In such a case, just return the mean.
            if (
                Double.IsNaN(rand_normal)
                || Double.IsInfinity(rand_normal)
                || Double.IsNegativeInfinity(rand_normal)
            ) {
                return mu;
            }

            return rand_normal;
        }
    }
}
