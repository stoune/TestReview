using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Server;
using NUnit.Framework;

namespace PrettyNumber
{
    public static class PrettyNumber
    {
        /// <summary>
        /// Maximum allowed number of bytes = 1 GB
        /// </summary>
        public const int MaxSupportedSize = 1000000000;

        private static readonly string[] Scales = {"B", "K", "M", "G"};

        /// <summary>
        /// Pretty format number of bytes
        /// </summary>
        /// <param name="numberOfBytes">number of bytes</param>
        /// <param name="maxSignificantDigits">Maximum of 3 digits (not counting a decimal point)</param>
        /// <returns>formatted string</returns>
        public static string Format(int numberOfBytes, int maxSignificantDigits = 3)
        {
            if (numberOfBytes < 0 ||
                numberOfBytes > MaxSupportedSize)
            {
                throw new ArgumentOutOfRangeException("numberOfBytes");
            }

            if (numberOfBytes == MaxSupportedSize)
            {
                return "1G";
            }

            if (numberOfBytes == 0)
            {
                return "0B";
            }

            int scaleIndex = 0;
            int remainder;
            // Find the right scale
            do
            {
                remainder = numberOfBytes%1000;
                numberOfBytes /= 1000;
                if (numberOfBytes > 0)
                {
                    ++scaleIndex;
                }
            } while (numberOfBytes > 1000);

            numberOfBytes = numberOfBytes*1000 + remainder;
            var decimalString = GetDecimalStringWithUnitOfMeasure(numberOfBytes, maxSignificantDigits, scaleIndex);
            return decimalString;
        }

        private static string GetDecimalStringWithUnitOfMeasure(int numberOfBytes, int maxSignificantDigits, int scaleIndex)
        {
            var converted = new StringBuilder(maxSignificantDigits);
            int iteration = 0;
            while (numberOfBytes != 0)
            {
                if (iteration == 3)
                {
                    // Insert decimal point
                    converted.Insert(0, ".");
                }
                int remInt = numberOfBytes%10;
                numberOfBytes /= 10;
                converted.Insert(0, remInt.ToString(CultureInfo.InvariantCulture));
                ++iteration;
            }

            // +1 - take into account decimal point symbol
            // Round to the nearest valid values.
            if (iteration > 3 && converted.Length > (maxSignificantDigits + 1) &&
                converted[maxSignificantDigits + 1] >= '5')
            {
                bool carry = false;
                for (int i = maxSignificantDigits; i >= 0; i--)
                {
                    // carry rounding thorough '.'
                    if (converted[i] == '.')
                    {
                        continue;
                    }

                    int digit = converted[i] - '0' + 1;

                    // Carry happen to the next digit
                    if (digit == 10)
                    {
                        carry = true;
                        digit = 0;
                    }
                    else
                    {
                        carry = false;
                    }

                    converted[i] = (char)('0' + digit);

                    if (!carry)
                    {
                        break;
                    }
                }

                if (carry)
                {
                    // Insert 1 at first position if carry was moved through all digits
                    converted.Insert(0, "1");
                }
            }

            // Return either whole string if it not exceed maximum allowed digits or
            // substring with Maximum of maxSignificantDigits digits
            var resultString = converted.ToString();

            //
            if (!resultString.Contains('.') && resultString.Length > maxSignificantDigits + 1)
            {
                // If after rounding we exceed current scale then move to the next scale, start from 1
                ++scaleIndex;
                resultString = "1";
            }
            else if (resultString.Contains('.') && resultString.Length > maxSignificantDigits + 1)
            {
                if (resultString.Substring(0, resultString.IndexOf('.')).Length > maxSignificantDigits)
                {
                    // If after rounding we exceed current scale then move to the next scale, start from 1
                    ++scaleIndex;
                    resultString = "1";
                }
                else
                {
                    // just trim everything that exceed allowed length
                    resultString = resultString.Substring(0, maxSignificantDigits + 1);
                    // No trailing zeroes after a decimal point.
                    resultString = resultString.TrimEnd('0');
                    resultString = resultString.TrimEnd('.');    
                }
            }

            return string.Format("{0}{1}", resultString, Scales[scaleIndex]);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(PrettyNumber.Format(303));
            Console.WriteLine(PrettyNumber.Format(30));
            Console.WriteLine(PrettyNumber.Format(0));
            Console.WriteLine(PrettyNumber.Format(341));
            Console.WriteLine(PrettyNumber.Format(34200));
            Console.WriteLine(PrettyNumber.Format(5910000));
            Console.WriteLine(PrettyNumber.Format(PrettyNumber.MaxSupportedSize));
            Console.WriteLine(PrettyNumber.Format(54123));
            Console.WriteLine(PrettyNumber.Format(1000));
            Console.WriteLine(PrettyNumber.Format(10054));
            // Rounding
            Console.WriteLine(PrettyNumber.Format(5915000));
            Console.WriteLine(PrettyNumber.Format(567900));
            Console.WriteLine(PrettyNumber.Format(PrettyNumber.MaxSupportedSize - 3));
            Console.WriteLine(PrettyNumber.Format(99999));
        }
    }

    /// <summary>
    /// Unit tests for calculating products of array
    /// <remarks>
    /// Please note that you will need NUnit to run these tests
    /// </remarks>
    /// </summary>
    [TestFixture]
    internal class PrettyNumberTests
    {
        [Test]
        public void Print341BTest()
        {
            Assert.AreEqual("341B", PrettyNumber.Format(341));
        }

        [Test]
        public void Print1GBTest()
        {
            Assert.AreEqual("1G", PrettyNumber.Format(PrettyNumber.MaxSupportedSize));
        }
    }
}
