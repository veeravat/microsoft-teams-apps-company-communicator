using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.AdaptiveCard.TaskModule
{
    public class UISettings
    {
        public UISettings(int width, int height, string title, string id, string buttonTitle)
        {
            Width = width;
            Height = height;
            Title = title;
            Id = id;
            ButtonTitle = buttonTitle;
        }

        public int Height { get; set; }
        public int Width { get; set; }
        public string Title { get; set; }
        public string ButtonTitle { get; set; }
        public string Id { get; set; }
    }
}
