﻿using System.Threading.Tasks;
using Atles.Models.Public.Index;

namespace Atlas.Client.Components.Themes
{
    public abstract class IndexComponent : ThemeComponentBase
    {
        protected IndexPageModel Model { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            Model = await ApiService.GetFromJsonAsync<IndexPageModel>("api/public/index-model");
        }
    }
}