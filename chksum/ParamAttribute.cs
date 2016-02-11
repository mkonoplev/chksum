using System;

namespace SuperWave.ChkSum
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class ParamAttribute : Attribute
    {
        #region Member variables.

        #endregion

        #region Properties.

        public string[] Aliases { get; private set; }
        public string Description { get; private set; }
        public bool IsRequired { get; private set; }

        #endregion

        #region Constructors and destructors.

        public ParamAttribute(string[] aliases, string description, bool isRequired)
        {
            Aliases = aliases;
            Description = description;
            IsRequired = isRequired;
        }

        #endregion
    }
}
