using AletheiaUI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
            Argument = new Argument();
        }

        public void OnPost()
        {
            Console.WriteLine("estou aqui");
        }

    }
}
