﻿using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Graph;
using Microsoft.Extensions.Logging;

using CsvHelper;

using PermafnotesDomain.Models;
using PermafnotesDomain.Services;
using AntDesign;

namespace PermafnotesRepositoryByFile
{
    public class Repositoy : IPermafnotesRepository
    {
        private static string s_noteFileDateTimeFormat = "yyyyMMddHHmmssfffffff";
        private static Encoding s_encoding = Encoding.GetEncoding("UTF-8");

        private IFileService _fileService;
        private ILogger _logger;

        private IEnumerable<NoteListModel> _noteRecords = new List<NoteListModel>();

        public static Repositoy CreateRepositoryUsingMsGraph(GraphServiceClient graphServiceClient, ILogger logger)
        {
            return new Repositoy(new MicrosoftGraphFileService(graphServiceClient, logger), logger);
        }

        public static Repositoy CreateRepositoryUsingFileSystem(ILogger logger, string baseDirectoryPath)
        {
            return new Repositoy(new FileSystemService(logger, baseDirectoryPath), logger);
        }

        private Repositoy(IFileService fileService, ILogger logger)
        {
            this._fileService = fileService;
            this._logger = logger;
        }

        public async Task<IEnumerable<NoteListModel>> Add(NoteListModel noteListModel)
        {
            this.AddToNoteRecords(noteListModel);

            await PutNoteListModel(noteListModel);

            return this.OrderByDescendingNoteRecords();
        }

        public async Task<IEnumerable<NoteListModel>> FetchAll(bool onlyCache = false)
        {
            this._noteRecords = await LoadCache();
            if (onlyCache)
                return this.OrderByDescendingNoteRecords();

            var children = await this._fileService.FetchChildren();
            List<NoteListModel> result = this._noteRecords.ToList();
            foreach (PermafnotesNoteFile child in children)
            {
                _logger.LogDebug($"Fetch start {child.Name}");
                if (result.Any(x => $"{x.Created.ToString(s_noteFileDateTimeFormat)}.json" == child.Name))
                {
                    _logger.LogDebug($"{child.Name}'s cache is exists");
                    continue;
                }

                _logger.LogWarning($"{child.Name} is not exists in cache. Loading this.");
                string text = await this._fileService.ReadNote(child.Name);
                NoteListModel? model = JsonSerializer.Deserialize<NoteListModel>(text);
                if (model is null)
                {
                    continue;
                }
                result.Add(model);
            }

            await SaveCache(result);
            this._noteRecords = result;

            return this.OrderByDescendingNoteRecords();
        }

        public async Task Export(IEnumerable<NoteListModel> records, string delimiter="\t")
        {
            StringBuilder sb = new(NoteListModel.BuildCsvHeader(delimiter));
            sb.Append("\n");
            records.ForEach(x => sb.Append(x.ToCsvLine(delimiter)).Append("\n"));
            string uploadName = $"{DateTime.Now.ToString(s_noteFileDateTimeFormat)}.tsv";

            await this._fileService.Export(uploadName, sb.ToString());
        }

        public async Task Import(byte[] inputBuffers)
        {
            using MemoryStream ms = new(inputBuffers);
            using StreamReader sr = new(ms, s_encoding);
            using CsvReader csv = new(sr, CultureInfo.InvariantCulture);

            foreach (var record in csv.GetRecords<NoteFormModel>())
            {
                await this.Add(new NoteListModel(record));
            }
        }

        public async IAsyncEnumerable<NoteTagModel> SelectAllTags()
        {
            List<NoteTagModel> returned = new();
            if (this._noteRecords.Count() <= 0)
            {
                await this.FetchAll(onlyCache: true);
            }
            foreach (var m in this._noteRecords)
            {
                foreach (var t in m.SplitTags())
                {
                    if (returned.Any(x => x.Name == t.Name))
                        continue;
                    returned.Add(t);
                    yield return t;
                }
            }
        }

        private IEnumerable<NoteListModel> OrderByDescendingNoteRecords()
        {
            return this._noteRecords.OrderByDescending(x => x.Created);
        }

        private async Task PutNoteListModel(NoteListModel noteListModel)
        {
            JsonSerializerOptions options = new()
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.
                Create(System.Text.Unicode.UnicodeRanges.All)
            };

            string uploadText = JsonSerializer.Serialize<NoteListModel>(noteListModel, options);
            string uploadName = $"{noteListModel.Created.ToString(s_noteFileDateTimeFormat)}.json";

            await this._fileService.WriteNote(uploadName, uploadText);

            // TODO: Let them concentrate only on writing notes. The cache should not have to be updated here.
            //var cache = await this.LoadCache();
            //if (cache.Any(x => x.Created == noteListModel.Created))
            //{
            //    var newCache = cache.Where(x => x.Created != noteListModel.Created).ToList();
            //    newCache.Add(noteListModel);
            //    _noteRecords = newCache;
            //    await SaveCache(_noteRecords);
            //}
        }

        private void AddToNoteRecords(NoteListModel noteListModel)
        {
            if (this._noteRecords is null)
            {
                this._noteRecords = new List<NoteListModel>();
            }

            var result = this._noteRecords.Where(x => x.Created != noteListModel.Created).ToList();
            result.Add(noteListModel);
            this._noteRecords = result;
        }

        private async Task<IEnumerable<NoteListModel>> LoadCache()
        {
            string text = await this._fileService.ReadCache();
            if (string.IsNullOrEmpty(text))
                return this.OrderByDescendingNoteRecords();

            List<NoteListModel>? result = JsonSerializer.Deserialize<List<NoteListModel>>(text);
            if (result is null)
                return this.OrderByDescendingNoteRecords();

            return result;
        }

        private async Task SaveCache(IEnumerable<NoteListModel> noteListModels)
        {
            JsonSerializerOptions options = new()
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.
                Create(System.Text.Unicode.UnicodeRanges.All)
            };

            string uploadText = JsonSerializer.Serialize<IEnumerable<NoteListModel>>(noteListModels, options);

            await this._fileService.WriteCache(uploadText);
        }
    }
}
