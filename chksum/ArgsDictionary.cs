using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SuperWave.ChkSum
{
    class ArgsDictionary : Dictionary<String, String>
    {
        #region Static and constants.

        #endregion

        #region Member variables.

        private readonly List<string> _files;

        #endregion

        #region Properties.

        public List<string> Files
        {
            get { return _files; }
        }

        #endregion

        #region Constructors and destructors.

        public ArgsDictionary(string[] args) : this()
        {
            Parse(args);
        }

        public ArgsDictionary()
        {
            _files = new List<string>();
        }

        #endregion

        #region Methods and implementations.

        public void Parse(params string[] args)
        {
            // Splits one argument into several parts: 'the parameter' and 'the value'.
            Regex splitter = new Regex(@"^-{1,2}|^/|=|:(?!\\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            // Is used to detect and remove all starting and trailing ' or " characters from a value
            Regex remover = new Regex(@"^['""]?(.*?)['""]?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            //
            string key = null;
            // Valid parameters forms: {-,/,--}param{ ,=,:}((",')value(",'))
            // Examples: 
            //      -param1 value1 --param2 /param3:"Test-:-work" /param4=happy -param5 '--=nice=--'
            for (int i = 0; i < args.Length; i++)
            {
                string[] split = splitter.Split(args[i], 3);

                // Look for new parameters (-,/ or --) and a possible enclosed value (=,:).
                switch (split.Length)
                {
                    case 1 /* found a value (for the last parameter found (space separator)) */:
                        if (null != key)
                        {
                            Add(key, remover.Replace(split[0], "$1"));
                            key = null;
                            break;
                        }
                        _files.Add(split[0]);
                        break;
                    case 2 /* found just a parameter */:
                        Add(split[1], String.Empty);
                        key = split[1];
                        break;
                    case 3 /* parameter with enclosed value */:
                        Add(split[1], remover.Replace(split[2], "$1"));
                        key = null;
                        break;
                }
            }         
        }

        #endregion
    }
}
