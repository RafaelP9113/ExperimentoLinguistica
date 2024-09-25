using ExperimentoLinguistica.Models;
using Microsoft.AspNetCore.Mvc;

namespace ExperimentoLinguistica.Controllers
{
    public class ExperimentoController : Controller
    {
        public IActionResult BoasVindas()
        {
            return View();
        }

        public IActionResult Consentimento()
        {
            return View(new Consentimento());
        }

        [HttpPost]
        public IActionResult Consentimento(Consentimento model)
        {
            if (model.Aceito)
            {
                return RedirectToAction("Instrucoes");
            }
            else
            {
                return RedirectToAction("BoasVindas");
            }
        }

        public IActionResult Instrucoes()
        {
            return View();
        }

        public IActionResult Experimento()
        {
            var estimulos = ObterEstimulos();
            return View(estimulos);
        }

        private List<Estimulo> ObterEstimulos()
        {
            return new List<Estimulo>
        {
            new Estimulo { Prime = "Pincel", Alvo = "苹果", TempoPrime = 50, TempoAlvo = 3000 },
            new Estimulo { Prime = "Café", Alvo = "家具", TempoPrime = 50, TempoAlvo = 3000 },

        };
        }

        public IActionResult Final()
        {
            return View();
        }
    }

}
