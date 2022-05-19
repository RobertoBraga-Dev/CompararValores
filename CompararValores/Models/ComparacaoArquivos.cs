namespace CompararValores.Models
{
    public class ComparacaoArquivos
    {
        public ComparacaoArquivos()
        {
            ValoresFile1 = new List<decimal>();
            ValoresFile2 = new List<decimal>();
        }
        public string NameFile1 { get; set; } = String.Empty;
        public string NameFile2 { get; set; } = String.Empty;
        public IList<decimal> ValoresFile1 { get; set; }
        public IList<decimal> ValoresFile2 { get; set; }
    }
}
