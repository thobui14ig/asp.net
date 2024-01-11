using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Printing;
using System.Web;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection;
using DevExpress.Pdf;


public struct PrinterOptions
{
    public string[] Collate;
    public string[] Duplexing;
    public short MaxCopy;
    public bool SupportsColor;
    public string[] PaperSheets;
    public string[] Resolutions;
}


public class PrinterInfo
{
    public string PrinterName { get; set; }
    public List<PaperSizeInfo> PaperSizes { get; set; }
}

public class PaperSizeInfo
{
    public string paperName { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
}

public class PrinterRequest
{
    public string PrinterName { get; set; }
}

public struct Settings
{
    //PrinterSettings
    public string Duplex { get; set; }

    //PageSettings
    public bool Landscape { get; set; }
    public string printer { get; set; }
    public string filename { get; set; }

    public string width { get; set; }

    public string height { get; set; }
}

namespace printer_2.Controllers
{
    [ApiController]
    [Route("api/printer")]
    public class PrinterController : ControllerBase
    {
        [HttpPost]
        [Route("info")]
        public IActionResult GetPrinterInfo([FromBody] PrinterRequest request)
        {
            PrintJson(request);
            string printerName = request.PrinterName;
            PrinterSettings printerSettings = new PrinterSettings
            {
                PrinterName = printerName
            };

            if (!PrinterSettings.InstalledPrinters.Cast<string>().Any(p => p.Equals(printerName, StringComparison.OrdinalIgnoreCase)))
            {
                return NotFound($"Printer '{printerName}' not found.");
            }


            PrinterInfo printerInfo = new PrinterInfo
            {
                PrinterName = printerName,
                PaperSizes = GetDefaultPaperSizes(printerSettings)
            };

            return Ok(printerInfo);
        }

        [HttpPost]
        [Route("test")]
        public Boolean handlePrint([FromBody] Settings request)
        {
            return Print(request);
        }

        private List<PaperSizeInfo> GetDefaultPaperSizes(PrinterSettings printerSettings)
        {
            List<PaperSizeInfo> paperSizes = new List<PaperSizeInfo>();

            foreach (PaperSize paperSize in printerSettings.PaperSizes)
            {
                PaperSizeInfo sizeInfo = new PaperSizeInfo
                {
                    paperName = paperSize.PaperName,
                    Width = paperSize.Width,
                    Height = paperSize.Height
                };

                paperSizes.Add(sizeInfo);
            }

            return paperSizes;
        }

        public static bool Print(Settings settings)
        {
            var printer = settings.printer;
            var filename = settings.filename;
            PrinterSettings printerSettings = new PrinterSettings { PrinterName = printer };

            if (!printerSettings.IsValid)
            {
                printerSettings = new PrinterSettings(); //fallback to default printer
            }


            if (Enum.TryParse<Duplex>(settings.Duplex, out Duplex d))
                printerSettings.Duplex = d;


            PageSettings pageSettings = new PageSettings(printerSettings)
            {
                Margins = new Margins(0, 0, 0, 0),
                Landscape = settings.Landscape
            };


            //if (paperSize.Length > 0)
            //{
            //    if (paperSize.Contains("Custom") || paperSize.Contains("custom"))
            //    {
            //        int dot = paperSize.IndexOf('.');
            //        int x = paperSize.IndexOf('x');
            //        int width = Int32.Parse(paperSize.Substring(dot + 1, x - dot - 1));
            //        int height = Int32.Parse(paperSize.Substring(x + 1));
            //        pageSettings.PaperSize = new PaperSize("Custom", width, height);
            //    }
            //    else
            //    {
            //        foreach (PaperSize ps in printerSettings.PaperSizes)
            //        {
            //            if (ps.PaperName == paperSize)
            //            {
            //                pageSettings.PaperSize = ps;
            //                break;
            //            }
            //        }

            //    }
            //}

            string mimeType = MimeMapping.MimeUtility.GetMimeMapping(filename);

            switch (mimeType)
            {
                case "application/pdf":
                    return PrintPDF(filename, printerSettings, pageSettings);
                default:
                    return false;
            }
        }

        private static bool PrintPDF(string filename, PrinterSettings printerSettings, PageSettings pageSettings)
        {
            try
            {
                PdfDocumentProcessor documentProcessor = new PdfDocumentProcessor();
                documentProcessor.LoadDocument(filename);

                // Declare printer settings.
                PdfPrinterSettings pdfPrinterSettings = new PdfPrinterSettings();
                pdfPrinterSettings.PageOrientation = PdfPrintPageOrientation.Portrait;
                //pdfPrinterSettings.PageOrientation = PdfPrintPageOrientation.Landscape;

                // Specify the custom scale number
                pdfPrinterSettings.ScaleMode = PdfPrintScaleMode.CustomScale;
                pdfPrinterSettings.Scale = 90;
                pdfPrinterSettings.Settings.DefaultPageSettings.PaperSize = new PaperSize("Custom", 20, 40);


                // Specify .NET printer settings
                PrinterSettings settings = pdfPrinterSettings.Settings;

                PrintJson(pdfPrinterSettings);

                // Print the document
                documentProcessor.Print(pdfPrinterSettings);

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        public static bool PrintJson(object printerOptions)
        {
            Console.Write(JsonConvert.SerializeObject(printerOptions, Formatting.None,
                           new JsonSerializerSettings()
                           {
                               ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                           }));
            return true;
        }
    }
}
