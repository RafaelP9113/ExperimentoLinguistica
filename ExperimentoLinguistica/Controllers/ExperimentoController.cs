using ExperimentoLinguistica.Models;
using Microsoft.AspNetCore.Mvc;
using NPOI.HSSF.Record.PivotTable;
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

            var consentimento = new Consentimento()
            {
                Guid = CreateGuidWithTime()
            };

            return View(consentimento);
        }

        public IActionResult Avaliacao(string idioma, string guidView, string listaView)
        {
            var listaItens = ObterListaItens(idioma, listaView);

            var model = new AvaliacaoViewModel
            {
                Guid = guidView,
                Idioma = idioma,
                Lista = listaView,
                ListaItens = listaItens
            };

            return View(model);
        }

        public static string CreateGuidWithTime()
        {
            byte[] guidBytes = Guid.NewGuid().ToByteArray();

            long ticks = DateTime.Now.Ticks;

            byte[] timeBytes = BitConverter.GetBytes(ticks);

            Array.Copy(timeBytes, 0, guidBytes, 0, 8);

            return new Guid(guidBytes).ToString();
        }


        [HttpPost]
        public IActionResult Consentimento(Consentimento model)
        {
            if (model.Aceito)
            {
                return RedirectToAction("Instrucoes", "Experimento", new { idioma = model.IdiomaSelecionado, guid = model.Guid });
            }
            else
            {
                return RedirectToAction("BoasVindas");
            }
        }

        public IActionResult Instrucoes(string idioma, string guid)
        {
            ViewBag.IdiomaSelecionado = idioma;
            ViewBag.Guid = guid;
            return View();
        }

        public IActionResult Experimento(string idioma, string guidView)
        {
            ViewBag.IdiomaSelecionado = idioma;
            ViewBag.Guid = guidView;
            ViewBag.Lista = ObterLista(idioma);
            return View();
        }

        public IActionResult Final(string idioma, string guid)
        {
            return View();
        }


        private List<Item> ObterListaItens(string idioma, string lista)
        {
            var itens = new List<Item>();

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lista.xlsx");

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
                        var a = row.GetCell(6)?.ToString();

                        if (row.GetCell(5)?.ToString() == idioma && row.GetCell(6)?.ToString() == lista)
                        {
                            string texto = row.GetCell(0)?.ToString();
                            string exemplo = row.GetCell(4)?.ToString();

                            itens.Add(new Item
                            {
                                Alvo = exemplo,
                                Prime = texto
                            });
                        }
                    }
                }
            }

            return itens;
        }

        [HttpGet]
        public IActionResult ObterTextos(string diretorio, string idioma)
        {
            string filePath = string.Empty;

            string? lista = ObterLista(idioma);

            if (diretorio == "Treino")
            {
                filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "treino.xlsx");
                lista = "";
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
                        var a = row.GetCell(6)?.ToString();

                        if (row.GetCell(5)?.ToString() == idioma && row.GetCell(6)?.ToString() != lista)
                        {
                            string texto = row.GetCell(0)?.ToString();
                            string simbolo1 = row.GetCell(1)?.ToString();
                            string simbolo2 = row.GetCell(2)?.ToString();
                            string simbolo3 = row.GetCell(3)?.ToString();
                            string exemplo = row.GetCell(4)?.ToString();

                            textos.Add(new string[] { texto, simbolo1, simbolo2, simbolo3, exemplo, lista });
                        }

                    }
                }
            }

            return Json(textos);
        }

        private static string ObterLista(string idioma)
        {
            var arquivoAvaliacao = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "resultado_avaliacao.xlsx");

            var lista = string.Empty;

            using (var stream = new FileStream(arquivoAvaliacao, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = new XSSFWorkbook(stream);
                ISheet sheet = workbook.GetSheetAt(0);
                int rowCount = sheet.LastRowNum;

                if (rowCount != 0)
                {
                    IRow row = sheet.GetRow(rowCount);

                    if (row != null)
                    {
                        var idiomaCelula = row.GetCell(4)?.ToString();

                        if (!string.IsNullOrEmpty(idiomaCelula) && idiomaCelula == idioma)
                        {
                            lista = row.GetCell(5)?.ToString();
                        }
                        else
                        {
                            while (idiomaCelula != idioma || rowCount == 0)
                            {
                                rowCount--;
                                row = sheet.GetRow(rowCount);

                                if (row != null)
                                {
                                    idiomaCelula = row.GetCell(4)?.ToString();
                                }
                            }

                            row = sheet.GetRow(rowCount);

                            if (row != null)
                            {
                                lista = row.GetCell(5)?.ToString();
                            }
                        }
                    }
                }
                else
                {
                    lista = "B";
                }
            }

            if (string.IsNullOrEmpty(lista))
            {
                lista = "B";
            }

            return lista;
        }

        [HttpPost]
        public async Task<IActionResult> SalvarAudio(string diretorio, string idioma, string guid, string lista)
        {
            var file = Request.Form.Files[0];
            var frase = int.Parse(Request.Form["frase"]);

            if (file != null && file.Length > 0)
            {
                string fileName = GerarNomeArquivo(frase, diretorio, idioma, guid, lista);
                string filePath = Path.Combine(_audioDirectory, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return Ok(new { mensagem = "Áudio salvo com sucesso.", nomeArquivo = fileName });
            }

            return BadRequest("Falha ao receber o arquivo de áudio.");
        }

        private string GerarNomeArquivo(int frase, string diretorio, string idioma, string guid, string lista)
        {
            if (!Directory.Exists(_audioDirectory))
            {
                Directory.CreateDirectory(_audioDirectory);
            }

            if (lista == "A")
            {
                lista = "B";
            }
            else
            {
                lista = "A";
            }

            string sufixo = diretorio == "Treino" ? "T" : "E";

            if (sufixo == "T")
            {
                return $"P{guid}F{frase}{sufixo}{idioma[0]}.wav";
            }
            else
            {
                return $"P{guid}F{frase}{sufixo}{idioma[0]}L{lista}.wav";
            }
        }

        [HttpPost]
        public IActionResult SalvarAvaliacao(string guid, string idioma, string lista, List<int> avaliacoes, List<Item> listaItens)
        {
            string caminhoArquivo = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "resultado_avaliacao.xlsx");

            IWorkbook workbook;
            ISheet sheet;

            using (var file = new FileStream(caminhoArquivo, FileMode.Open, FileAccess.Read))
            {
                workbook = new XSSFWorkbook(file);
            }

            sheet = workbook.GetSheetAt(0);

            int rowCount = sheet.LastRowNum + 1;

            for (int i = 0; i < avaliacoes.Count; i++)
            {
                var row = sheet.CreateRow(rowCount++);

                row.CreateCell(0).SetCellValue(guid);
                row.CreateCell(1).SetCellValue(listaItens[i].Prime);
                row.CreateCell(2).SetCellValue(listaItens[i].Alvo);
                row.CreateCell(3).SetCellValue(avaliacoes[i]);
                row.CreateCell(4).SetCellValue(idioma);
                row.CreateCell(5).SetCellValue(lista);
            }

            using (var file = new FileStream(caminhoArquivo, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(file);
            }

            return RedirectToAction("Final");
        }

    }

}
