using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using OfficeOpenXml;

namespace Lay_Artificial_Manager
{
    internal class Program
    {
        public static string columns_names = "Statement Period|Transaction Month|Label|Artist|Release Title|Track Title|UPC|ISRC|Release Catalog ID|Track Catalog ID|Format|ParentSaleId|Transaction ID|Account ID|Contract ID|Payee ID|Service|Channel|Territory|Quantity|Gross Revenue in USD|Mechanical Royalties Deducted|Contract Rate %|Net Revenue in USD|Your Share %|US WHT Deducted|Amount Due in USD|Opening Balance in USD|Closing Balance in USD";
        private static int progress = 0;
        public static List<DSP> dsps = new List<DSP>();

        public class DSP
        {
            public string name { get; set; }
            public List<DSP_YEAR> years { get; set; }
        }

        public class DSP_YEAR
        {
            public string year { get; set; }
            public List<DSP_MONTH> months { get; set; }
        }

        public class DSP_MONTH
        {
            public string month { get; set; }
            public List<string> data { get; set; }
        }

        static void Main(string[] args)
        {
            try
            {
                string[] allfilesADDREPORT = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "NEW REPORTS"), "*.csv*", SearchOption.AllDirectories);

                foreach (string file in allfilesADDREPORT)
                {
                    Console.Title = "Lay Artificial Manager [ADD REPORTS]         Progress [ " + progress + " | " + allfilesADDREPORT.Length + " ]";
                    Console.WriteLine(file);
                    ProcessFile(file);

                    progress++;
                }

                Console.Title = "Lay Artificial Manager [ADD REPORTS]         Progress [ " + progress + " | " + allfilesADDREPORT.Length + " ]";
                Console.WriteLine(" DONE");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadKey();
            }
        }

        static void ProcessFile(string file)
        {
            List<string> lines = File.ReadAllLines(file).ToList();
            foreach (string line in lines)
            {
                try
                {
                    string modifiedLine = ModifyLine(line);
                    if (IsValidLine(modifiedLine))
                    {
                        UpdateDSP(modifiedLine);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("\nException processing line: \n\n" + line);
                    Console.WriteLine(ex.Message);
                }
            }
            Console.WriteLine("\n");
            foreach (DSP dsp in dsps)
            {
                SaveDSP(dsp);
                Console.WriteLine("Saved " + dsp.name);
            }
        }

        static string ModifyLine(string line)
        {
            string modifiedLine = line.Replace("/", " ");

            string pattern = ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)";
            modifiedLine = Regex.Replace(modifiedLine, pattern, "|");

            pattern = "(\\d)\\.(\\d)";
            modifiedLine = Regex.Replace(modifiedLine, pattern, "$1,$2");

            modifiedLine = modifiedLine.Replace("\"", "");

            return modifiedLine;
        }

        static bool IsValidLine(string line)
        {
            string[] parts = line.Split('|');
            return parts.Length > 1 && parts[1].Contains("-");
        }

        static void UpdateDSP(string line)
        {
            string[] parts = line.Split('|');
            string dspName = parts[16];
            string year = parts[1].Split('-')[0];
            string month = parts[1].Split('-')[1];

            DSP existingDSP = dsps.Find(x => x.name == dspName);
            if (existingDSP == null)
            {
                existingDSP = new DSP { name = dspName, years = new List<DSP_YEAR>() };
                dsps.Add(existingDSP);
            }

            DSP_YEAR existingYear = existingDSP.years.Find(x => x.year == year);
            if (existingYear == null)
            {
                existingYear = new DSP_YEAR { year = year, months = new List<DSP_MONTH>() };
                existingDSP.years.Add(existingYear);
            }

            DSP_MONTH existingMonth = existingYear.months.Find(x => x.month == month);
            if (existingMonth == null)
            {
                existingMonth = new DSP_MONTH { month = month, data = new List<string>() };
                existingYear.months.Add(existingMonth);
            }

            if (!existingMonth.data.Contains(line))
            {
                existingMonth.data.Add(line);
            }
        }

        static void SaveDSP(DSP dsp)
        {
            try
            {
                foreach (DSP_YEAR dspYEAR in dsp.years)
                {
                    foreach (DSP_MONTH dspMONTH in dspYEAR.months)
                    {

                        string dir = Path.Combine(Directory.GetCurrentDirectory(), "DB", dspYEAR.year, dspMONTH.month);
                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }

                        string fileName = Path.Combine(dir, dsp.name + ".csv");

                        using (StreamWriter writer = new StreamWriter(fileName))
                        {
                            writer.WriteLine(columns_names);
                            foreach (string line in dspMONTH.data)
                            {
                                writer.WriteLine(line);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occurred while saving DSP data:");
                Console.WriteLine(ex.Message);
                Console.ReadKey();
            }
        }
    }
}
