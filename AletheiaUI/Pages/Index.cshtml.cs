using AletheiaUI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace AletheiaUI.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        [BindProperty]
        public Argument Argument { get; set; }


        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
            Argument = new Argument
            {
                SourceDirectory = @"C:\Users\carla\Downloads\opencv-4.0.1\opencv-4.0.1\samples\winrt\FaceDetection\FaceDetection",
                ProjectPath = @"C:\Users\carla\Downloads\opencv-4.0.1\opencv-4.0.1\samples\winrt\FaceDetection\FaceDetection\FaceDetection.vcxproj"
            };
        }

        public async Task<IActionResult> OnGet()
        {

            return this.Page();
        }

        public async Task<IActionResult> OnPost()
        {
            Console.WriteLine("estou aqui");
            try
            {


                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "CMD.exe",
                        Arguments = "../../../Aletheia/bin/x64/Debug/Aletheia.exe do=getHelp",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = false,
                    }
                };

                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                startInfo.UseShellExecute = true;
                startInfo.Arguments = "/C copy /b Image1.jpg + Archive.rar Image2.jpg";
                process.StartInfo = startInfo;
                process.Start();

                proc.Start();

            }
            catch (Exception e )
            {
                Console.WriteLine(e.Message);
                throw;
            }

            return await this.OnGet();
        }

    }
}
