using CheckListApp.Models;
using CommunityToolkit.Mvvm.Input;

namespace CheckListApp.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}