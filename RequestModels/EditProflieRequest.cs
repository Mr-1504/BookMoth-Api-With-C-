using Microsoft.AspNetCore.Mvc;

namespace BookMoth_Api_With_C_.RequestModels
{
    public class EditProflieRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Username { get; set; }
        public IFormFile? Avatar { get; set; }
        public IFormFile? Cover { get; set; }
        public int? Gender { get; set; }
        public bool? Identifier { get; set; }
        public string? Birth { get; set; }
    }
}
