using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using IronPython.Hosting;

namespace SuperWave.ChkSum
{
    class Program
    {
        #region Static and constants.

        private const string StrUsage =
            "Usage:\r\n" +
            "\tchksum.exe  [-hashAlgorithm <hash alogorithm>] [-macro <macro>] file [file...]\r\n" +
            "\r\n" +
            "Parameters:\r\n" +
            "\t-hashAlgorithm <hash alogorithm> Cryptographic hash algorithm.\r\n" +
            "\t-macro <macro>                   Custom macro.\r\n" +
            "\t-?                               Displays help at the command prompt.\r\n" +
            "Where:\r\n" +
            "\t<hash alogorithm>:\r\n" +
            "\t\tMD5:                           \"MD5\"\r\n" +
            "\t\tSHA1:                          \"SHA1\"\r\n" +
            "\t\tSHA256:                        \"SHA256\"\r\n" +
            "Note:\r\n" +
            "\tCommand-line options are case-sensitive.\r\n";

        private static readonly string[] HlpKeys =
            new string[] {"-?", "--?", "/?", "-h", "--h", "/h", "-help", "--help", "/help",};

        private const string StrMd5 = "MD5";
        private const string StrSha1 = "SHA1";
        private const string StrSha256 = "SHA256";

        private const string StrComputeHash = "ComputeHash";

        #endregion

        #region Methods and implementations.

        private static byte[] ComputeHash(string hashAlgorithmName, string fileName)
        {
            HashAlgorithm hashAlgorithm = null;
            switch (hashAlgorithmName)
            {
                case StrMd5:
                    hashAlgorithm = new MD5Cng();
                    break;
                case StrSha1:
                    hashAlgorithm = new SHA1Cng();
                    break;
                case StrSha256:
                    hashAlgorithm = new SHA256Cng();
                    break;
            }
            if (null != hashAlgorithm)
            {
                using (var stream = File.OpenRead(fileName))
                {
                    return hashAlgorithm.ComputeHash(stream);
                }
            }
            var message = String.Format("Invalid hash algorithm name: {0}", hashAlgorithmName);
            throw new ApplicationException(message);
        }

        private static byte[] ComputeHash(string hashAlgorithmName, string macro, string fileName)
        {
            var scriptEngine = Python.CreateEngine();
            var scriptScope = scriptEngine.CreateScope();
            var scriptSource = scriptEngine.CreateScriptSourceFromFile(macro);
            scriptSource.Execute(scriptScope);
            var computeHash = 
                scriptScope.GetVariable<Func<string, string, byte[]>>(StrComputeHash);
            return computeHash(hashAlgorithmName, fileName);
        }

        static Cfg Args2Cfg(ArgsDictionary argsDictionary)
        {
            var cfg = new Cfg();

            FieldInfo[] fields = (typeof(Cfg)).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                var paramAttribute = 
                    field.GetCustomAttribute(typeof (ParamAttribute), false) as ParamAttribute;
                if (null == paramAttribute)
                {
                    continue;
                }
                bool isValueSet = false;
                foreach (var alias in paramAttribute.Aliases)
                {
                    if (argsDictionary.ContainsKey(alias))
                    {
                        if (typeof(String) == field.FieldType)
                        {
                            field.SetValue(cfg, argsDictionary[alias]);
                            isValueSet = true;
                        }
                        else
                        {
                            var message = String.Format( "Invalid command line: {0}. Make sure that you have not duplicated any command-line arguments, " +
                                "make sure that you have not used any arguments that are not valid.", Environment.CommandLine);
                            throw new ApplicationException(message);
                        }
                    }
                }
                if (!isValueSet && paramAttribute.IsRequired)
                {
                    var message = String.Format(
                        "Parameter: '{0}' is required. Run with no arguments to displays help at the command prompt.", paramAttribute.Aliases[0]);
                    throw new ApplicationException(message);
                }
            }
            return cfg;

        }

        private static void Main(string[] args)
        {
            //args = new string[]
            //{
            //    "C:\\inetpub\\wwwroot\\Анкета.Редактор\\Анкета.Редактор.xml",
            //    "-h",
            //};

            if (0 == args.Length)
            {
                Console.WriteLine(StrUsage);
                return;
            }
            if ((from arg in args from hlpKey in HlpKeys 
                 where 0 == String.Compare(arg, hlpKey, StringComparison.OrdinalIgnoreCase) select arg).Any())
            {
                Console.WriteLine(StrUsage);
                return;
            }
            try
            {
                var argsDictionary = new ArgsDictionary(args);
                var cfg = Args2Cfg(argsDictionary);

                foreach (var file in argsDictionary.Files)
                {
                    if (!File.Exists(file))
                    {
                        WriteLine(TraceEventType.Error, null,
                            "{0}: was not found or the caller does not have the required permission.", file);
                        continue;
                    }
                    try
                    {
                        byte[] hash;
                        Dictionary<string, string> extendedProperties;

                        if (!String.IsNullOrEmpty(cfg.Macro))
                        {
                            // Macro.
                            hash = ComputeHash(String.IsNullOrEmpty(cfg.HashAlgorithm) ? StrMd5 : cfg.HashAlgorithm, cfg.Macro, file);
                            //extendedProperties = new Dictionary<string, string>
                            //{
                            //    { "HashAlgorithmName", String.IsNullOrEmpty(cfg.HashAlgorithm) ? StrMd5 : cfg.HashAlgorithm },
                            //    { "Macro", cfg.Macro },
                            //};
                            // A string of hexadecimal pairs separated by hyphens, where each pair represents
                            // the corresponding element in value; for example, "7F-2C-4A-00". 
                            WriteLine(TraceEventType.Information,
                                null, "{0}: {1}", file, BitConverter.ToString(hash).Replace("-", String.Empty));
                            continue;
                        }
                        // 
                        hash = ComputeHash(String.IsNullOrEmpty(cfg.HashAlgorithm) ? StrMd5 : cfg.HashAlgorithm, file);
                        //extendedProperties = new Dictionary<string, string>
                        //{
                        //    { "HashAlgorithmName", String.IsNullOrEmpty(cfg.HashAlgorithm) ? StrMd5 : cfg.HashAlgorithm },
                        //};
                        // A string of hexadecimal pairs separated by hyphens, where each pair represents
                        // the corresponding element in value; for example, "7F-2C-4A-00". 
                        WriteLine(TraceEventType.Information,
                            null, "{0}: {1}", file, BitConverter.ToString(hash).Replace("-", String.Empty));

                    }
                    catch (Exception exception)
                    {
                        WriteLine(TraceEventType.Error, exception.Data, "{0}: {1}", file, exception.Message);
                    }
                }
            }
            catch (Exception exception)
            {
                WriteLine(TraceEventType.Error, exception.Data, exception.Message);
            }
        }

        private static void WriteLine(TraceEventType severity, IDictionary extendedProperties, string format, params object[] args)
        {
            Trace.WriteLine(String.Format("[{0}][{1}] {2}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), severity, String.Format(format, args)));
            if (null != extendedProperties)
            {
                foreach (DictionaryEntry dictionaryEntry in extendedProperties)
                {
                    Trace.WriteLine(String.Format("                     [{0}] {1}: {2}",
                        severity, dictionaryEntry.Key, dictionaryEntry.Value));
                }
            }
        }

        #endregion
    }
}
