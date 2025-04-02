using BookMoth_Api_With_C_.Models;
using BookMoth_Api_With_C_.ResponseModels;

namespace BookMoth_Api_With_C_.Services
{
    public class ProfileService : IProfileService
    {
        private readonly BookMothContext _context;

        public ProfileService(BookMothContext context)
        {
            _context = context;
        }

        public async Task<List<ProfileDTO>> SearchUsersByFollowAsync(int profileId, string searchString)
        {
            return await _context.SearchUsersByFollowAsync(profileId, searchString);
        }
    }

    public interface IProfileService
    {
        Task<List<ProfileDTO>> SearchUsersByFollowAsync(int userId, string searchString);
    }
}
