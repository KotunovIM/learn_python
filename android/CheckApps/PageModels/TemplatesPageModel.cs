using CheckApps.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;

namespace CheckApps.PageModels;

public class TemplatesPageModel
{
    public ObservableCollection<Checklist> Templates { get; } = new()
    {
        new Checklist
        {
            Name = "Пожарная безопасность",
            AccentColor = Colors.OrangeRed,
            Items = new List<ChecklistItem>
            {
                new() { Text = "Проверить огнетушители" },
                new() { Text = "Проверить выходы" },
                new() { Text = "Проверить сигнализацию" }
            }
        },
        new Checklist
        {
            Name = "Охрана труда",
            AccentColor = Colors.SteelBlue,
            Items = new List<ChecklistItem>
            {
                new() { Text = "Проверить СИЗ" },
                new() { Text = "Проверить инструктаж" }
            }
        },
        new Checklist
        {
            Name = "Электробезопасность",
            AccentColor = Colors.SeaGreen,
            Items = new List<ChecklistItem>
            {
                new() { Text = "Проверить щиты" },
                new() { Text = "Проверить заземление" }
            }
        }
    };
}
