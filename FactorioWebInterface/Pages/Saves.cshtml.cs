using FactorioWebInterface.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FactorioWebInterface.Pages
{
    public class SavesModel : PageModel
    {
        public FileMetaData[] StartSaves { get; private set; }
        public FileMetaData[] FinalSaves { get; private set; }
        public FileMetaData[] OldSaves { get; private set; }


        public IActionResult OnGet(string directory, string file)
        {
            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(file))
            {
                StartSaves = PublicFactorioSaves.GetFiles(Constants.PublicStartSavesDirectoryName) ?? new FileMetaData[0];
                FinalSaves = PublicFactorioSaves.GetFiles(Constants.PublicFinalSavesDirectoryName) ?? new FileMetaData[0];
                OldSaves = PublicFactorioSaves.GetFiles(Constants.PublicOldSavesDirectoryName) ?? new FileMetaData[0];

                return Page();
            }

            var fi = PublicFactorioSaves.GetFile(directory, file);

            if (fi == null)
            {
                return Unauthorized();
            }

            return File(fi.OpenRead(), "application/zip", fi.Name);
        }
    }
}