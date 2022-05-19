using ClosedXML.Excel;
using CompararValores.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CompararValores.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CompararExcel(IFormFile compareFile1, string compareColumn1, IFormFile compareFile2, string compareColumn2)
        {
            if (compareFile1 == null || compareFile2 == null)
                return RedirectToAction("Index");

            var compfiles = new ComparacaoArquivos
            {
                NameFile1 = compareFile1.FileName,
                NameFile2 = compareFile2.FileName
            };

            compfiles.ValoresFile1 = RetornaValoresColuna(compareFile1, compareColumn1);
            compfiles.ValoresFile2 = RetornaValoresColuna(compareFile2, compareColumn2);


            for (int i = compfiles.ValoresFile1.Count - 1; i >= 0; i--)
            {
                if (compfiles.ValoresFile2.Contains(compfiles.ValoresFile1[i]))
                {
                    compfiles.ValoresFile2.Remove(compfiles.ValoresFile1[i]);
                    compfiles.ValoresFile1.RemoveAt(i);
                }
            }

            return View(compfiles);
        }


        private List<decimal> RetornaValoresColuna(IFormFile file, string coluna)
        {
            var lista = new List<decimal>();
            using (var workbook = new XLWorkbook(file.OpenReadStream()))
            {
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.Column(coluna).CellsUsed().Select(l => l.Address.RowNumber);
                foreach (var row in rows)
                {
                    if (decimal.TryParse(worksheet.Row(row).Cell(coluna).Value.ToString(), out decimal valor))
                        if (valor < 0)
                            valor = valor * -1;
                    if (valor > 0)
                        lista.Add(valor);
                }
            }

            return lista;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}