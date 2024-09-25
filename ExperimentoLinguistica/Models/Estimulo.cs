using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExperimentoLinguistica.Models
{
    public class Estimulo
    {
        public string Prime { get; set; }
        public string Alvo { get; set; }
        public int TempoPrime { get; set; }
        public int TempoAlvo { get; set; }
    }
}
