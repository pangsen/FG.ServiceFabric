using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Newtonsoft.Json;

namespace FG.ServiceFabric.Data
{
    public interface IExternalActorStateProvider : IActorStateProvider
    {
        Task<bool> ContainsExternalStateAsync(ActorId actorId, string stateName, CancellationToken cancellationToken = new CancellationToken());
        Task RestoreExternalState<T>(ActorId actorId, string stateName, CancellationToken cancellationToken = new CancellationToken());
    }

    public class FileStore : WrappedActorStateProvider, IExternalActorStateProvider
    {
        private readonly JsonSerializerSettings _settings;
        private const string BaseFolderPath = @"C:\Temp\";
        
        public FileStore(ActorTypeInformation actorTypeInfor, IActorStateProvider stateProvider = null) 
            : base(actorTypeInfor, stateProvider)
        {
            _settings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };
        }

        private static string GetFolderPath(ActorId actorId)
        {
            var folder = Path.Combine(BaseFolderPath, actorId.ToString(), "Actors");
            Directory.CreateDirectory(folder);
            return folder;
        }
        private static string GetFolderPath()
        {
            var folder = Path.Combine(BaseFolderPath, "ActorService");
            Directory.CreateDirectory(folder);
            return folder;
        }

        public Task RestoreExternalState<T>(ActorId actorId, string stateName,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var filePath = Path.Combine(GetFolderPath(actorId), stateName + ".json");

            if (File.Exists(filePath))
            {
                var content = File.ReadAllText(filePath);
                var externalState = JsonConvert.DeserializeObject<T>(content, _settings);

                if (externalState != null)
                {
                    // Set lost state.
                    return base.SaveStateAsync(actorId,
                        new[] { new ActorStateChange(stateName, typeof(T), externalState, StateChangeKind.Add) },
                        cancellationToken);
                }
            }
            
            return Task.FromResult(false);
        }
        
        public async Task<bool> ContainsExternalStateAsync(ActorId actorId, string stateName,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return await Task.Run(() => File.Exists(Path.Combine(GetFolderPath(actorId), stateName + ".json")), cancellationToken);
        }

        public override async Task SaveStateAsync(ActorId actorId, IReadOnlyCollection<ActorStateChange> stateChanges,
            CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                //todo:transactional
                foreach (var actorStateChange in stateChanges)
                {
                    switch (actorStateChange.ChangeKind)
                    {
                        case StateChangeKind.None:
                            break;
                        case StateChangeKind.Add:
                            var addData = JsonConvert.SerializeObject(actorStateChange.Value, Formatting.Indented, _settings);
                            await Task.Run(() => File.WriteAllText(Path.Combine(GetFolderPath(actorId), actorStateChange.StateName + ".json"), addData), cancellationToken);
                            break;
                        case StateChangeKind.Update:
                            var updateData = JsonConvert.SerializeObject(actorStateChange.Value, Formatting.Indented, _settings);
                            await Task.Run(() => File.WriteAllText(Path.Combine(GetFolderPath(actorId), actorStateChange.StateName + ".json"), updateData), cancellationToken);
                            break;
                        case StateChangeKind.Remove:
                            await Task.Run(() => File.Delete(Path.Combine(GetFolderPath(actorId), actorStateChange.StateName + ".json")), cancellationToken);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(actorStateChange.ChangeKind));
                    }
                }
                await base.SaveStateAsync(actorId, stateChanges, cancellationToken);
            }
            catch (Exception e)
            {
                throw;
            }
            
        }
        
        public override async Task<T> LoadStateAsync<T>(ActorId actorId, string stateName, CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                if (!await base.ContainsStateAsync(actorId, stateName, cancellationToken) &&
                   await ContainsExternalStateAsync(actorId, stateName, cancellationToken))
                {
                    await RestoreExternalState<T>(actorId, stateName, cancellationToken);
                }

                return await base.LoadStateAsync<T>(actorId, stateName, cancellationToken);
            }
            catch (Exception e)
            {
                throw;
            }   
        }

        public override Task<IActorReminderCollection> LoadRemindersAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return base.LoadRemindersAsync(cancellationToken);
        }

        public Task RestoreExternalReminders(CancellationToken cancellationToken = new CancellationToken())
        {
            var folderPath = Path.Combine(GetFolderPath(), ".json");

            foreach (var fileName in Directory.EnumerateFiles(folderPath))
            {
                if (!fileName.Contains("Reminder"))
                    continue;

                var content = File.ReadAllText(Path.Combine(folderPath,fileName));
                var externalState = JsonConvert.DeserializeObject<ExternalReminder>(content, _settings);

                if (externalState != null)
                {
                    // Set lost state.
                    return base.SaveReminderAsync(
                        externalState.ActorId,
                        externalState,
                        cancellationToken);
                }
            }
            
            return Task.FromResult(false);
        }
        
        public override async Task SaveReminderAsync(ActorId actorId, IActorReminder reminder,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var externalReminder = new ExternalReminder()
            {
                ActorId = actorId,
                DueTime = reminder.DueTime,
                Name = reminder.Name,
                Period = reminder.Period,
                State = reminder.State
            };

            var reminderData = JsonConvert.SerializeObject(externalReminder, Formatting.Indented, _settings);
            await Task.Run(() => File.WriteAllText(Path.Combine(GetFolderPath(), "Reminder_" + actorId + reminder.Name + ".json"), reminderData), cancellationToken);

            await base.SaveReminderAsync(actorId, reminder, cancellationToken);
        }

        private class ExternalReminder : IActorReminder
        {
            public ActorId ActorId { get; set; }
            public string Name { get; set; }
            public TimeSpan DueTime { get; set; }
            public TimeSpan Period { get; set; }
            public byte[] State { get; set; }
        }

        public override Task DeleteReminderAsync(ActorId actorId, string reminderName,
            CancellationToken cancellationToken = new CancellationToken())
        {
            File.Delete(Path.Combine(GetFolderPath(), "Reminder", actorId + reminderName + ".json"));
            return base.DeleteReminderAsync(actorId, reminderName, cancellationToken);
        }
    }
}