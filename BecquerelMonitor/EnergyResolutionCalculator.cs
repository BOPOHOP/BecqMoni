﻿using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace BecquerelMonitor
{
    // Token: 0x02000086 RID: 134
    public class EnergyResolutionCalculator
    {
        // Token: 0x060006DF RID: 1759 RVA: 0x000285F0 File Offset: 0x000267F0
        public static EnergyResolutionResult CalculateFWHM(EnergySpectrum spectrum, double startEnergy, double endEnergy)
        {
            EnergyCalibration energyCalibration = spectrum.EnergyCalibration;
            int startChannel = (int)Math.Ceiling(energyCalibration.EnergyToChannel(startEnergy, maxChannels: spectrum.NumberOfChannels));
            int endChannel = (int)Math.Floor(energyCalibration.EnergyToChannel(endEnergy, maxChannels: spectrum.NumberOfChannels));
            return EnergyResolutionCalculator.CalculateFWHM(spectrum, startChannel, endChannel);
        }

        public static int doCorrection(EnergySpectrum energySpectrum, int centroid, int low_boundary, int high_boundary)
        {
            if (low_boundary < 0) low_boundary = 0;
            if (high_boundary >= energySpectrum.NumberOfChannels) high_boundary = energySpectrum.NumberOfChannels - 1;
            int poly_order = 8;
            if (high_boundary - low_boundary < 8)
            {
                poly_order = high_boundary - low_boundary;
            }
            if (poly_order < 3)
            {
                return (int)Math.Max(energySpectrum.Spectrum[low_boundary], energySpectrum.Spectrum[high_boundary]);
            }
            double[] x = new double[high_boundary - low_boundary + 1];
            double[] y = new double[high_boundary - low_boundary + 1];
            for (int j = 0; j < high_boundary - low_boundary + 1; j++)
            {
                x[j] = low_boundary + j;
                y[j] = energySpectrum.Spectrum[low_boundary + j];
            }
            Func<double, double> func = Fit.PolynomialFunc(x, y, poly_order);
            double new_centroid = centroid;
            double max = func.Invoke(new_centroid);
            for (int j = low_boundary; j < high_boundary; j++)
            {
                double new_max = func.Invoke(j);
                if (new_max > max)
                {
                    new_centroid = j;
                    max = new_max;
                }
            }
            return (int)Math.Round(new_centroid);
        }



        // Token: 0x060006E0 RID: 1760 RVA: 0x0002862C File Offset: 0x0002682C
        public static EnergyResolutionResult CalculateFWHM(EnergySpectrum spectrum, int startChannel, int endChannel)
        {
            EnergyCalibration energyCalibration = spectrum.EnergyCalibration;
            if (startChannel >= endChannel)
            {
                return null;
            }
            EnergyResolutionCalculator.result.StartChannel = (double)startChannel;
            EnergyResolutionCalculator.result.EndChannel = (double)endChannel;
            int num = startChannel;
            if (spectrum.Spectrum.Length <= startChannel) return null;
            double num2 = (double)spectrum.Spectrum[startChannel];
            for (int i = startChannel; i <= endChannel; i++)
            {
                if ((double)spectrum.Spectrum[i] > num2)
                {
                    num2 = (double)spectrum.Spectrum[i];
                    num = i;
                }
            }

            int cent = num;
            if (endChannel - startChannel > 8)
            {
                try
                {
                    cent = doCorrection(spectrum, num, startChannel, endChannel);
                } catch
                {
                    cent = num;
                }
                
                if (cent > endChannel || cent < startChannel) cent = num;
            }


            EnergyResolutionCalculator.result.MaxChannel = (double)cent;
            EnergyResolutionCalculator.result.MaxValue = (double)spectrum.Spectrum[(int)EnergyResolutionCalculator.result.MaxChannel];

            Trace.WriteLine(EnergyResolutionCalculator.result.MaxChannel + " cent: " + cent);

            double num3 = (double)spectrum.Spectrum[startChannel];
            double num4 = (double)spectrum.Spectrum[endChannel];
            double num5 = num3 + (num4 - num3) * (double)(num - startChannel) / (double)(endChannel - startChannel);
            EnergyResolutionCalculator.result.StartValue = num3;
            EnergyResolutionCalculator.result.EndValue = num4;
            EnergyResolutionCalculator.result.MaxBaseValue = num5;
            double num6 = (num2 - num5) / 2.0 + num5;
            EnergyResolutionCalculator.result.HalfValue = num6;
            double num7 = -1.0;
            int j = startChannel + 1;
            while (j < num)
            {
                if ((double)spectrum.Spectrum[j] > num6)
                {
                    if (spectrum.Spectrum[j] == spectrum.Spectrum[j - 1])
                    {
                        return null;
                    }
                    num7 = (double)(j - 1) + (num6 - (double)spectrum.Spectrum[j - 1]) / (double)(spectrum.Spectrum[j] - spectrum.Spectrum[j - 1]);
                    break;
                }
                else
                {
                    j++;
                }
            }
            if (num7 < 0.0)
            {
                return null;
            }
            double num8 = -1.0;
            int k = endChannel - 1;
            while (k > num)
            {
                if ((double)spectrum.Spectrum[k] > num6)
                {
                    if (spectrum.Spectrum[k] == spectrum.Spectrum[k + 1])
                    {
                        return null;
                    }
                    num8 = (double)(k + 1) - (num6 - (double)spectrum.Spectrum[k + 1]) / (double)(spectrum.Spectrum[k] - spectrum.Spectrum[k + 1]);
                    break;
                }
                else
                {
                    k--;
                }
            }
            if (num8 < 0.0)
            {
                return null;
            }
            EnergyResolutionCalculator.result.LeftChannel = num7;
            EnergyResolutionCalculator.result.RightChannel = num8;
            double num9 = energyCalibration.ChannelToEnergy(num7);
            double num10 = energyCalibration.ChannelToEnergy(num8);
            double num11 = energyCalibration.ChannelToEnergy((double)num);
            double resolution = (num10 - num9) / num11;
            double resolutioninkev = num10 - num9;
            EnergyResolutionCalculator.result.Resolution = resolution;
            EnergyResolutionCalculator.result.ResolutionInkeV = resolutioninkev;
            return EnergyResolutionCalculator.result;
        }

        // Token: 0x04000394 RID: 916
        static EnergyResolutionResult result = new EnergyResolutionResult();
    }
}
