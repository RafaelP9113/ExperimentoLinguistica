namespace ExperimentoLinguistica.Models
{
    public class AvaliacaoViewModel
    {
        public string Guid { get; set; }
        public string Idioma { get; set; }
        public string Lista { get; set; }
        public List<int> Avaliacoes { get; set; }
        public List<Item> ListaItens { get; set; }

        public AvaliacaoViewModel()
        {
            Avaliacoes = new List<int>();
            ListaItens = new List<Item>();
        }
    }

}
