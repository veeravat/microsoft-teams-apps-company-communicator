using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.AdaptiveCard.TaskModule
{
    public static class TaskModuleUIConstants
    {
        public static UISettings YouTube { get; set; } =
            new UISettings(1000, 700, "You Tube Video", TaskModuleIds.YouTube, "You Tube");
        public static UISettings CustomForm { get; set; } =
            new UISettings(510, 450, "Custom Form", TaskModuleIds.CustomForm, "Custom Form");
        public static UISettings AdaptiveCard { get; set; } =
            new UISettings(510, 450, "Message Details", TaskModuleIds.AdaptiveCard, "CC Message");
    }
}
