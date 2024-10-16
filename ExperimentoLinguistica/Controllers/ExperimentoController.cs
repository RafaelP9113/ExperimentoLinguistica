using ExperimentoLinguistica.Models;
using Microsoft.AspNetCore.Mvc;
using NAudio.Wave;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Vosk;

namespace ExperimentoLinguistica.Controllers
{
    public class ExperimentoController : Controller
    {
        private readonly string _audioDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "audios");

        public IActionResult BoasVindas()
        {
            return View();
        }

        public IActionResult TestMic(string idioma)
        {
            ViewBag.IdiomaSelecionado = idioma;

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
            string guid = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

            while (CheckIfGuidExists(guid))
            {
                guid = Guid.NewGuid().ToString("N").Substring(0, 8);
            }

            return guid;
        }

        public static bool CheckIfGuidExists(string guid)
        {
            var exists = false;

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "resultado_tempoReacao.xlsx");

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
                        string guidarq = row.GetCell(0)?.ToString();

                        if (!string.IsNullOrEmpty(guidarq))
                        {
                            if (guidarq == guid)
                            {
                                exists = true;
                                break;
                            }
                        }
                    }
                }
            }

            return exists;
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
            ViewBag.IdiomaSelecionado = idioma;
            ViewBag.Guid = guid;
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

            string? lista = string.Empty;


            if (diretorio == "Treino")
            {
                filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "treino.xlsx");
                lista = "";
            }
            else
            {
                lista = ObterLista(idioma);
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

            var groupedByLength = textos.GroupBy(t => t[0].Length).ToList();

            Random rng = new Random();
            foreach (var group in groupedByLength)
            {
                group.ToList().Shuffle(rng);
            }

            var result = new List<string[]>();
            int maxGroupSize = groupedByLength.Max(g => g.Count());

            for (int i = 0; i < maxGroupSize; i++)
            {
                foreach (var group in groupedByLength)
                {
                    if (i < group.Count())
                    {
                        result.Add(group.ElementAt(i));
                    }
                }
            }

            return Json(result);
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
                            while (idiomaCelula != idioma || rowCount <= 0)
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
        public async Task<IActionResult> SalvarAudio(string diretorio, string idioma, string guid, string lista, string tempoReacao)
        {
            var file = Request.Form.Files[0];
            var frase = Request.Form["frase"].ToString();

            if (file != null && file.Length > 0)
            {

                string fileName = GerarNomeArquivo(frase, diretorio, idioma, guid, lista, tempoReacao);
                string filePath = Path.Combine(_audioDirectory, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                string recognizedText = RecognizeSpeechFromFile(filePath);
                double accuracy = CompareText(recognizedText, frase);

                return Ok(new { mensagem = "Áudio salvo com sucesso.", nomeArquivo = fileName });
            }

            return BadRequest("Falha ao receber o arquivo de áudio.");
        }
        private string GerarNomeArquivo(string frase, string diretorio, string idioma, string guid, string lista, string tempoReacao)
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
                SalvarTempoReacao(guid, frase, idioma, lista, tempoReacao);
                return $"P{guid}F{frase}{sufixo}{idioma[0]}L{lista}.wav";
            }
        }

        private void SalvarTempoReacao(string guid, string frase, string idioma, string lista, string tempoReacao)
        {
            string caminhoArquivo = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "resultado_tempoReacao.xlsx");

            IWorkbook workbook;
            ISheet sheet;

            using (var file = new FileStream(caminhoArquivo, FileMode.Open, FileAccess.Read))
            {
                workbook = new XSSFWorkbook(file);
            }

            sheet = workbook.GetSheetAt(0);

            int rowCount = sheet.LastRowNum + 1;

            var row = sheet.CreateRow(rowCount++);

            row.CreateCell(0).SetCellValue(guid);
            row.CreateCell(1).SetCellValue(frase);
            row.CreateCell(2).SetCellValue(idioma);
            row.CreateCell(3).SetCellValue(lista);
            row.CreateCell(4).SetCellValue(tempoReacao);

            using (var file = new FileStream(caminhoArquivo, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(file);
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

            return RedirectToAction("Final", new { idioma = idioma, guid = guid });
        }

        public static string RecognizeSpeechFromFile(string audioFilePath)
        {
            try
            {
                Vosk.Vosk.SetLogLevel(0);
                var modelPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "vosk-model-small-ja-0.22");
                var model = new Vosk.Model(modelPath);

                using (var waveReader = new WaveFileReader(audioFilePath))
                {
                    var sampleRate = waveReader.WaveFormat.SampleRate;
                    var recognizer = new VoskRecognizer(model, sampleRate);

                    byte[] buffer = new byte[waveReader.WaveFormat.SampleRate / 2];
                    int bytesRead;

                    while ((bytesRead = waveReader.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        recognizer.AcceptWaveform(buffer, bytesRead);
                    }

                    return recognizer.FinalResult();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static double CompareText(string recognizedText, string expectedText)
        {
            int length = Math.Min(recognizedText.Length, expectedText.Length);
            int matches = 0;

            for (int i = 0; i < length; i++)
            {
                if (recognizedText[i] == expectedText[i])
                {
                    matches++;
                }
            }

            return (double)matches / expectedText.Length * 100.0; 
        }

    }


    public static class Extensions
    {
        private static Random rng = new Random();
        public static void Shuffle<T>(this IList<T> list, Random rng)
        {
            int n = list.Count;
            while (n > 1)
            {
                int k = rng.Next(n--);
                T temp = list[n];
                list[n] = list[k];
                list[k] = temp;
            }
        }
    }
}

