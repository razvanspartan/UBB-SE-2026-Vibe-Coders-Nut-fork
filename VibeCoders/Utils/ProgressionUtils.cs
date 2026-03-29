using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VibeCoders.Models;

namespace VibeCoders.Utils
{
    public static class ProgressionUtils
    {
        public static double CalculateRatio(int act, int target)
        {
            if (target == 0) return 0.0;
            return (double)act / target;
        }

        public static double DetermineWeightIncrement(MuscleGroup group)
        {
            return group == MuscleGroup.LEGS ? 5.0 : 2.5;
        }

        public static double CalculateDeload(double currentWeight)
        {
            double deloaded = currentWeight * 0.9;
            return Math.Round(deloaded * 2) / 2.0;
        }
    }
}