using FirebaseAdmin.Messaging;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

namespace BookMoth_Api_With_C_.Services
{
    public class FcmService
    {
        private FirebaseApp _firebaseApp;

        public void InitializeFirebase()
        {
            if (_firebaseApp == null)
            {
                _firebaseApp = FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromFile("./Resources/bookmoth-firebase.json") 
                });
            }
        }

        public async Task SendNotificationAsync(string token, string title, string body)
        {
            InitializeFirebase();

            var message = new Message()
            {
                Token = token, // Token của thiết bị nhận tin nhắn
                Notification = new Notification()
                {
                    Title = title,
                    Body = body
                }
            };

            string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            Console.WriteLine("Message ID: " + response);
        }
    }
}
