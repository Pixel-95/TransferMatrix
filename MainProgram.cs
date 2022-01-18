using System;
using System.IO;
using System.Linq;

namespace TransferMatrix
{
    class MainProgram
    {
        static string filepathInput = @"../../../dataInput/";
        static string filepathOutput = @"../../../dataOutput/";

        static void Main(string[] args)
        {
            //  ██╗ materials
            //  ╚═╝
            Material air = new Material("air", 0, new PropertiesOptics(filepathInput + "nk_data_air.txt"));
            Material GaInAs = new Material("GaInAs", 0, new PropertiesOptics(filepathInput + "nk_data_GaInAs.txt"), "data from S. Adachi, Journal of Applied Physics 66, 6030-6040 (1989)") ;
            Material AZO = new Material("AZO", 0, new PropertiesOptics(filepathInput + "nk_data_AZO.txt"), "data from R. E. Treharne et al, Journal of Physics 286, 012038 (2011)");
            Material Ag = new Material("Ag", 0, new PropertiesOptics(filepathInput + "nk_data_Ag.txt"), "data from Y. Jiang et al, Scientific Reports 6, 1-7 (2016)") ;

            Console.Write("Calculation ...");
            (Material material, double thickness, double roughnessOnTop)[] materialStack;
            Material materialBeforeStack;
            (Material, double) materialBehindStack;
            materialStack = new (Material material, double thickness, double roughnessOnTop)[]
            {
                (AZO, 500e-9, 0e-9),
                (GaInAs, 1000e-9, 0e-9),
            };
            materialBeforeStack = air;
            materialBehindStack = (Ag, 0e-9);
            ModelTMM modelOptics = new ModelTMM(materialBeforeStack, materialBehindStack, materialStack, MiscTMM.spectrumAM15);
            Console.WriteLine(" done.\n");

            //  ██╗ depth-dependent
            //  ╚═╝
            Console.Write("Exporting depth-dependent data ...");
            using (StreamWriter file = new StreamWriter(filepathOutput + "depthDependentData.dat", false))
            {
                file.WriteLine("depth\tpoynting vector s\tpoynting vector p\tpoynting vector\tabsorption");
                file.WriteLine("nm\tW/m^2\tW/m^2\tW/m^2\t1/(s*m^3)");

                for (double d = -modelOptics.lengthOfStack * 0.1e9; d <= modelOptics.lengthOfStack * 1.1e9; d += modelOptics.lengthOfStack * 0.005e9)
                {
                    var poynting = modelOptics.GetPoyntingVectorAtPosition(d * 1e-9);
                    file.WriteLine(d + "\t" + poynting.s + "\t" + poynting.p + "\t" + (poynting.p + poynting.s) + "\t" + modelOptics.GetLocalAbsorption(d * 1e-9));
                }

                file.Close();
            }
            Console.WriteLine(" done.");

            //  ██╗ wavelength-dependent
            //  ╚═╝
            Console.Write("Exporting wavelength-dependent data ...");
            using (StreamWriter file = new StreamWriter(filepathOutput + "fractionOfLight.dat", false))
            {
                file.Write("wavelength\tAM1.5G");
                foreach (var layer in modelOptics.layerStack.Reverse())
                    file.Write("\t" + layer.material.name);
                file.WriteLine("\treflected\ttransmitted");
                foreach (var wavelengthEQE in modelOptics.GetOpticalEQE())
                {
                    file.Write(wavelengthEQE.wavelength + "\t");

                    file.Write(MiscTMM.spectrumAM15.SpectralIntensityDensityAtWavelength(wavelengthEQE.wavelength).spectralIntensityDensity + "\t");

                    foreach (var layer in wavelengthEQE.absorbed.Reverse())
                        file.Write(layer + "\t");

                    file.Write(wavelengthEQE.reflected + "\t");
                    file.WriteLine(wavelengthEQE.transmitted);
                }
            }
            Console.WriteLine(" done.");

            //  ██╗ angle-dependent
            //  ╚═╝
            Console.Write("Exporting angle-dependent data ...");
            using (StreamWriter file = new StreamWriter(filepathOutput + "reflected.dat", false))
            {
                file.WriteLine("angle\treflected s\treflected p");
                file.WriteLine("°\t%\t%");

                for (double alpha = 0; alpha < 90; alpha += 1.0)
                {
                    modelOptics = new ModelTMM(materialBeforeStack, materialBehindStack, materialStack, MiscTMM.spectrumAM15Simple, 1, alpha);
                    double Rs = 0, Rp = 0, norm = 0;
                    for (int i = 0; i < modelOptics.R_s.Length; i++)
                    {
                        norm += modelOptics.spectrum.data[i].lambda;
                        Rs += modelOptics.R_s[i] * modelOptics.spectrum.data[i].lambda;
                        Rp += modelOptics.R_p[i] * modelOptics.spectrum.data[i].lambda;
                    }
                    Rs /= norm;
                    Rp /= norm;
                    file.WriteLine(alpha + "\t" + Rs * 100 + "\t" + Rp * 100);
                }
                file.WriteLine("90\t100\t100");

                file.Close();
            }
            Console.WriteLine(" done.");

            Console.WriteLine("\nEntire program done.\nAll files are exported to: " + Path.GetFullPath(filepathOutput) + "...\nThis window can be closed.");
            Console.ReadLine();
        }
    }
}
