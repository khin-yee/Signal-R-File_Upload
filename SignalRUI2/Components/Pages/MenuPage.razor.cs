using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace SignalRUI2.Components.Pages
{
    public partial class MenuPage:ComponentBase
    {
        public class ChatItem
        {
            public string Name { get; set; }
            public string LastMessage { get; set; }
            public DateTime Time { get; set; }
            public string AvatarUrl { get; set; }
        }
        [Inject] public NavigationManager navigationManager { get; set; }
        List<ChatItem> ChatList = new()
        {
            new ChatItem { Name = "Alice", LastMessage = "Hey, how are you?", Time = DateTime.Now.AddMinutes(-5), AvatarUrl = "https://i.pravatar.cc/150?img=1" },
            new ChatItem { Name = "Bob", LastMessage = "Meeting at 3 PM.", Time = DateTime.Now.AddHours(-1), AvatarUrl = "https://i.pravatar.cc/150?img=2" },
            new ChatItem { Name = "Charlie", LastMessage = "Let's catch up tomorrow.", Time = DateTime.Now.AddHours(-2), AvatarUrl = "https://i.pravatar.cc/150?img=3" },
        };

        void OpenChat(ChatItem chat)
        {
            navigationManager.NavigateTo("/signalRTest2");
        }
    }
}
