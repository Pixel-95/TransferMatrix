using Extreme.Mathematics;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransferMatrix
{
    public class PropertiesOptics
    {
        /// <summary>
        /// lambda-dependent complex refractive index
        /// </summary>
        public (double lambda, Complex<double> n)[] n_rawData { get; private set; }

        /// <summary>
        /// lambda-dependent complex refractive index (index in this array is index in spectrum)
        /// </summary>
        public Complex<double>[] n_toSpectrum { get; private set; }

        public (double weight1, int ID1, double weight2, int ID2) originalMaterials { get; private set; } = (-1, -1, -1, -1);

        public string filepath { get; private set; }

        /// <summary>
        /// Constructor from filepath
        /// </summary>
        public PropertiesOptics(string filepath)
        {
            if (filepath != null)
            {
                var array = ReadLinesTo2DArray(ReadInputFile(filepath));
                n_rawData = new (double lambda, Complex<double> n)[array.GetLength(0)];
                for (int i = 0; i < array.GetLength(0); i++)
                    n_rawData[i] = (array[i, 0] * 1e-9, new Complex<double>(array[i, 1], array[i, 2]));
            }
            this.filepath = filepath;
        }

        /// <summary>
        /// set array for each wavelength in spectrum to array (performance reasons)
        /// </summary>
        /// <param name="spectrum">spectrum (lambda in meter, deltaLambda in meter, spectral intensity density in W/m^3)</param>
        public void InitializeNKarray((double lambda, double deltaLambda, double spectralIntensityDensity)[] spectrum)
        {
            n_toSpectrum = new Complex<double>[spectrum.Length];
            for (int specIndex = 0; specIndex < spectrum.Length; specIndex++)
                n_toSpectrum[specIndex] = n_rawData.MinBy(p => Math.Abs(p.lambda - spectrum[specIndex].lambda)).First().n;
        }

        /// <summary>
        /// Reads string lines and outputs it as a 2D-double-Array 
        /// ignores leading lines, where the first element cannot be parsed to a double (headerlines))
        /// </summary>
        /// <param name="lines">array of strings which are the lines, where the data is in</param>
        /// <param name="delimiterColumn">separator between columns</param>
        public static double[,] ReadLinesTo2DArray(string[] lines, char delimiterColumn = '\t')
        {
            // Get the amount of leading header lines by checking if first elements in rows are parsable
            int amountHeaderLines = 0;
            while (amountHeaderLines <= lines.Length)
                try
                { ToDoubleWithArbitrarySeparator(lines[amountHeaderLines].Split(delimiterColumn).First().Trim()); break; }
                catch
                { amountHeaderLines++; }

            // cut away the header lines
            lines = lines.Skip(amountHeaderLines).ToArray();

            // Amount of row-delimiters determines the first dimension
            int amountLines = lines.Where(l => !l.Trim().Equals(string.Empty)).Count();
            // Amount of column-delimiters IN THE FIRST LINE determines the second dimension
            int amountRows = lines.First().Split(delimiterColumn).Where(s => !s.Trim().Equals(string.Empty)).Count();

            // Create 2D Array
            double[,] matrix = new double[amountLines, amountRows];

            // Interation variables
            int i = 0, j;

            // Iterate through rows
            foreach (var row in lines)
            {
                // Iterate through columns
                j = 0;
                foreach (var element in row.Split(delimiterColumn))
                {
                    // Try to parse number
                    double number = 0;
                    try { number = ToDoubleWithArbitrarySeparator(element.Trim().Trim()); }
                    catch { /* Element i,j could not be parsed. */ }

                    // Try to set number
                    try { matrix[i, j] = number; }
                    catch { /* Out of range error: element i,j does not exist in lines x rows matrix. */ }

                    // Go to next column
                    j++;
                }

                // Go to next row
                i++;
            }

            // Return Array
            return matrix;
        }

        /// <summary>
        /// Reads a double number with , or . as decimal separator
        /// </summary>
        /// <param name="numberString">input number</param>
        /// <returns></returns>
        public static double ToDoubleWithArbitrarySeparator(string numberString)
        {
            if (CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator == ",")
                return Convert.ToDouble(numberString.Replace(".", ","));
            else
                return Convert.ToDouble(numberString.Replace(",", "."));
        }

        /// <summary>
        /// Read in parameter file (without comments)
        /// </summary>
        /// <param name="filepath">path of the parameter file</param>
        public static string[] ReadInputFile(string filepath)
        {
            // Only if file exists
            if (File.Exists(filepath))
            {
                // read file to list
                List<string> allLines = File.ReadAllLines(filepath).ToList();

                for (int counter = 0; counter < allLines.Count; counter++)
                {
                    // remove multi-line comments
                    if (allLines[counter].StartsWith("/*"))
                    {
                        int start = counter;
                        int stop;
                        for (int length = 0; true; length++)
                            if (allLines[counter + length].Contains("*/"))
                            {
                                stop = counter + length;
                                break;
                            }
                        allLines.RemoveRange(start, stop - start + 1);

                        counter -= counter - (stop - start + 1);
                    }
                }
                for (int counter = 0; counter < allLines.Count; counter++)
                {
                    // remove single-line comments
                    allLines[counter] = allLines[counter].Replace("//", "╰").Split('╰')[0];

                    // remove all empty lines (completely empty or only tab and space)
                    if (allLines[counter].Replace("\t", "").Replace(" ", "") == "")
                    {
                        allLines.RemoveAt(counter);
                        counter--;
                    }
                }

                return allLines.ToArray();
            }
            else
            {
                throw new Exception("File >>>" + filepath + "<<< does not exist!");
            }
        }
    }
}