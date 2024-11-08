﻿namespace PermafnotesDomain.Models;

// TODO: move to permafnotes
using System.ComponentModel.DataAnnotations;

public class NoteFormModel
{
    public long Id { get; set; } = 0;

    [Required(ErrorMessage = "Title is required")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Source is required")]
    public string Source { get; set; } = string.Empty;

    [Required(ErrorMessage = "Memo is required")]
    public string Memo { get; set; } = string.Empty;

    // TODO: implement delete duplicate item and empty item
    public IEnumerable<string> Tags { get; set; } = new List<string>();

    [Required(ErrorMessage = "Reference is required")]
    public string Reference { get; set; } = string.Empty;

    public DateTime Created { get; set; } = DateTime.MinValue;

    public bool HasUrlReference()
        => Reference.StartsWith("http://") || Reference.StartsWith("https://");

    public override string ToString()
        => $"{Title}, {Source}, {Memo}, {ConvertTagsToString()}, {Reference}";

    internal IEnumerable<NoteTagModel> ConvertTagsToString()
    {
        return Tags.Select(x => new NoteTagModel(x));
    }
}
