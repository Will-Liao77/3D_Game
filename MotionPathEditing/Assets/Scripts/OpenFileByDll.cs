using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Windows.Forms;

public class OpenFileByDll : MonoBehaviour
{
    public void OpenFile()
    {
        OpenFileDialog dialog = new OpenFileDialog();
        dialog.Filter = "All Files (*.*)|*.*";

        dialog.InitialDirectory = @"C:\";
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            string path = dialog.FileName;
            Debug.Log(path);
        }
    }
}
