using ExperimentoLinguistica.Models;
using Microsoft.AspNetCore.Mvc;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Collections.Generic;

namespace ExperimentoLinguistica.Controllers
{
    public class ExperimentoController : Controller
    {
        private readonly string _audioDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "audios");

        public IActionResult BoasVindas()
        {
            return View();
        }

        public IActionResult Consentimento(string idioma)
        {
            ViewBag.IdiomaSelecionado = idioma;
            return View(new Consentimento());
        }

        [HttpPost]
        public IActionResult Consentimento(Consentimento model)
        {
            if (model.Aceito)
            {
                return RedirectToAction("Instrucoes", "Experimento", new { idioma = model.IdiomaSelecionado });
            }
            else
            {
                return RedirectToAction("BoasVindas");
            }
        }

        public IActionResult Instrucoes(string idioma)
        {
            ViewBag.IdiomaSelecionado = idioma;
            return View();
        }

        public IActionResult Experimento(string idioma)
        {
            ViewBag.IdiomaSelecionado = idioma;

            return View();
        }

        public IActionResult Final()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ObterTextos(string diretorio, string idioma)
        {
            string filePath = string.Empty;

            if (diretorio == "Treino")
            {
                filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "treino.xlsx");
            }
            else
            {
                filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lista.xlsx");
            }

            var textos = new List<string[]>();

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = new XSSFWorkbook(stream);
                ISheet sheet = workbook.GetSheetAt(0);
                int rowCount = sheet.LastRowNum;

                for (int rowIndex = 1; rowIndex <= rowCount; rowIndex++)
                {
                    IRow row = sheet.GetRow(rowIndex);

                    if (row != null)
                    {
                        if (row.GetCell(4)?.ToString() == idioma)
                        {
                            string texto = row.GetCell(0)?.ToString();
                            string simbolo1 = row.GetCell(1)?.ToString();
                            string simbolo2 = row.GetCell(2)?.ToString();
                            string exemplo = row.GetCell(3)?.ToString();

                            textos.Add(new string[] { texto, simbolo1, simbolo2, exemplo });
                        }

                    }
                }
            }

            return Json(textos);
        }

        [HttpPost]
        public async Task<IActionResult> SalvarAudio(string diretorio, string idioma)
        {
            var file = Request.Form.Files[0];
            var frase = int.Parse(Request.Form["frase"]);

            if (file != null && file.Length > 0)
            {
                string fileName = GerarNomeArquivo(frase, diretorio, idioma);
                string filePath = Path.Combine(_audioDirectory, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return Ok(new { mensagem = "Áudio salvo com sucesso.", nomeArquivo = fileName });
            }

            return BadRequest("Falha ao receber o arquivo de áudio.");
        }

        private string GerarNomeArquivo(int frase,string diretorio, string idioma)
        {
            if (!Directory.Exists(_audioDirectory))
            {
                Directory.CreateDirectory(_audioDirectory);
            }

            string sufixo = diretorio == "Treino" ? "T" : "E";
            var arquivosExistentes = Directory.GetFiles(_audioDirectory, $"P*F{frase}{sufixo}{idioma[0]}.wav");

            int maiorP = 0;

            foreach (var arquivo in arquivosExistentes)
            {
                var nomeArquivo = Path.GetFileNameWithoutExtension(arquivo);

                var partes = nomeArquivo.Split('P', 'F', sufixo[0], idioma[0]);

                if (partes.Length >= 2 && int.TryParse(partes[1], out int numeroP))
                {
                    if (numeroP > maiorP)
                    {
                        maiorP = numeroP;
                    }
                }
            }

            int proximoP = maiorP + 1;

            return $"P{proximoP}F{frase}{sufixo}{idioma[0]}.wav";
        }
    }

}
