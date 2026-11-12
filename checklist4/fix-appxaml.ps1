$ErrorActionPreference = "Stop"
$path = "C:\GIT\github\CheckListApp\App.xaml"

$newContent = @'
<?xml version="1.0" encoding="UTF-8" ?>
<Application xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="CheckListApp.App">
    <Application.Resources>
        <ResourceDictionary>
            <Color x:Key="Primary">#1E90FF</Color>
            <Color x:Key="Danger">#E84118</Color>
            <Color x:Key="Bg">#F5F6FA</Color>
            <Color x:Key="CardBg">#FFFFFF</Color>
            <Color x:Key="TextMain">#2F3640</Color>
            <Color x:Key="TextMuted">#718093</Color>

            <!-- Базовый стиль кнопки с явным ключом -->
            <Style x:Key="BaseButton" TargetType="Button">
                <Setter Property="BackgroundColor" Value="{StaticResource CardBg}"/>
                <Setter Property="TextColor" Value="{StaticResource TextMain}"/>
                <Setter Property="BorderColor" Value="#DCDDE1"/>
                <Setter Property="BorderWidth" Value="1"/>
                <Setter Property="CornerRadius" Value="6"/>
                <Setter Property="Padding" Value="10,6"/>
                <Setter Property="FontSize" Value="14"/>
            </Style>

            <Style x:Key="PrimaryBtn" TargetType="Button" BasedOn="{StaticResource BaseButton}">
                <Setter Property="BackgroundColor" Value="{StaticResource Primary}"/>
                <Setter Property="TextColor" Value="White"/>
                <Setter Property="FontAttributes" Value="Bold"/>
            </Style>

            <Style x:Key="DangerBtn" TargetType="Button" BasedOn="{StaticResource BaseButton}">
                <Setter Property="BackgroundColor" Value="{StaticResource Danger}"/>
                <Setter Property="TextColor" Value="White"/>
                <Setter Property="FontAttributes" Value="Bold"/>
            </Style>

            <Style x:Key="SectionHeader" TargetType="Label">
                <Setter Property="FontSize" Value="15"/>
                <Setter Property="FontAttributes" Value="Bold"/>
                <Setter Property="TextColor" Value="{StaticResource TextMain}"/>
                <Setter Property="Margin" Value="0,8,0,4"/>
            </Style>
        </ResourceDictionary>
    </Application.Resources>
</Application>
'@

[System.IO.File]::WriteAllText($path, $newContent, (New-Object System.Text.UTF8Encoding $false))
Write-Host "App.xaml перезаписан. Убрал {x:Type Button}." -ForegroundColor Green

# Пересборка
Set-Location "C:\GIT\github\CheckListApp"
dotnet build -f net10.0-android -c Debug 2>&1 | Select-String "error|Build succeeded" | Select-Object -First 15