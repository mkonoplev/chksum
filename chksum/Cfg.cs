namespace SuperWave.ChkSum
{
    class Cfg
    {
        [Param(new string[] { "hashAlgorithm", "ha", }, "The cryptographic hash algorith.m", false)]
        public string HashAlgorithm;
        [Param(new string[] { "macro", "m", }, "The custom macro.", false)]
        public string Macro;
    }
}
