using FactorioWebInterface.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FactorioWebInterface.Pages
{
    public class SavesModel : PageModel
    {
        public PublicFileTableModel StartSaves { get; private set; }
        public PublicFileTableModel FinalSaves { get; private set; }
        public PublicFileTableModel OldSaves { get; private set; }

        public IActionResult OnGet(string directory, string file)
        {
            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(file))
            {
                StartSaves = new PublicFileTableModel()
                {
                    Id = "startSavesTable",
                    Saves = PublicFactorioSaves.GetFiles(Constants.PublicStartSavesDirectoryName) ?? new FileMetaData[0]
                };

                FinalSaves = new PublicFileTableModel()
                {
                    Id = "finalSavesTable",
                    Saves = PublicFactorioSaves.GetFiles(Constants.PublicFinalSavesDirectoryName) ?? new FileMetaData[0]
                };

                OldSaves = new PublicFileTableModel()
                {
                    Id = "oldSavesTable",
                    Saves = PublicFactorioSaves.GetFiles(Constants.PublicOldSavesDirectoryName) ?? new FileMetaData[0]
                };

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