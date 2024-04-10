using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace Project.Views;

public partial class MainView : UserControl
{
    private event Action<float> ProgressChanged;
    
    private float Progress
    {
        set => ProgressChanged.Invoke(value);
    }
    
    private void OnProgressChanged(float progress)
    {
        Dispatcher.UIThread.Post(() => ProgressBar.Value = progress);
    }

    private Task SendNotification(string msg)
    {
        return Task.Run(async () =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                var textBox = new TextBox
                {
                    Text = msg,
                    Classes =
                    {
                        "Notification"
                    }
                };
                GridNotification.Children.Add(textBox);
            });

            await Task.Delay(2000);

            Dispatcher.UIThread.Post(() => 
                GridNotification.Children.RemoveAt(GridNotification.Children.Count-1));
        });
    }

    private Task EncodeDecodeFile(string path, string key)
    {
        return Task.Run(() =>
        {
            var file = File.Open(path, FileMode.Open, FileAccess.Read);
            
            var newFile = File.Create($"{Path.GetDirectoryName(path)}/" +
                                      $"{Path.GetFileNameWithoutExtension(path)}1{Path.GetExtension(path)}");
        
            byte[] buffer = new byte[4096];
            int bytesRead;
        
            while ((bytesRead = file.Read(buffer, 0, buffer.Length)) > 0)
            {
                newFile.Write(EncodeDecodeBytes(buffer, key), 0, bytesRead);

                Progress = (float)newFile.Length / file.Length;
            }
        
            file.Dispose();
            newFile.Dispose();

            SendNotification("Файл готов!");
        });
    }
    
    private byte[] EncodeDecodeBytes(byte[] input, string key)
    {
        var inputBits = new BitArray(input);
        var keyBits = new BitArray(Encoding.UTF8.GetBytes(key));
        
        BitArray answer = new BitArray(inputBits.Length);

        int y = 0;
        for (int x = 0; x < inputBits.Length; x++)
        {
            if (y == keyBits.Length)
                y = 0;

            if (inputBits[x] == keyBits[y])
            {
                answer[x] = false;
            }
            else
            {
                answer[x] = true;
            }

            y++;
        }
        byte[] byteArray = new byte[(answer.Length + 7) / 8];

        answer.CopyTo(byteArray, 0);

        return byteArray;
    }
    
    private string EncodeDecodeString(string input, string key)
    {
        var inputBytes= Encoding.UTF8.GetBytes(input);

        byte[] bytes = EncodeDecodeBytes(inputBytes, key);
        
        return Encoding.UTF8.GetString(bytes);
    }
    
    private void TextBoxes_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (InputTextBox.Text is null)
            return;

        if (KeyTextBox.Text is null || KeyTextBox.Text.Length == 0)
        {
            SendNotification("Не введён ключ").ConfigureAwait(false);
            return;
        }

        if (InputTextBox.Text.Length == 0)
        {
            OutputTextBox.Text = String.Empty;
            return;
        }

        OutputTextBox.Text = EncodeDecodeString(InputTextBox.Text, KeyTextBox.Text);
    }
    
    private void Drop(object sender, DragEventArgs e)
    {
        if (KeyTextBox.Text is null || KeyTextBox.Text.Length == 0)
        {
            SendNotification("Не введён ключ").ConfigureAwait(false);
            return;
        }
        
        var files = e.Data.GetFiles()?.ToArray();

        if (files is null || files.Length != 1)
            return;

        EncodeDecodeFile(files[0].Path.LocalPath, KeyTextBox.Text).ConfigureAwait(false);
    }
    
    public MainView()
    {
        InitializeComponent();
        
        DragDrop.SetAllowDrop(this, true);
        
        InputTextBox.AddHandler(DragDrop.DropEvent, Drop);

        ProgressChanged += OnProgressChanged;
    }
}