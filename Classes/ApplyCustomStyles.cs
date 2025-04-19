using System;
using System.Drawing;
using System.Windows.Forms;

namespace SUBR
{
    class ApplyCustomStyles
    {
        public void ApplyCustomStyle(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is Button button)
                {
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderSize = 1;
                    button.FlatAppearance.BorderColor = Color.DodgerBlue; // Default blue border
                    button.BackColor = Color.FromArgb(20, 20, 40); // Default dark blue
                    button.ForeColor = Color.White;
                    button.TabStop = false;

                    // 🔥 Special case: If button text contains "delete", make it RED
                    if (button.Text.ToLower().Contains("delete"))
                    {
                        button.ForeColor = Color.Red;
                    }

                    // 🔥 Special case: Submit or Edit Existing buttons — make them ORANGE
                    if (button.Name.ToLower().Contains("submit") || button.Name.ToLower().Contains("editexistingsystem"))
                    {
                        button.BackColor = Color.FromArgb(255, 140, 0); // Orange background
                        button.FlatAppearance.BorderColor = Color.Orange;
                        button.ForeColor = Color.White;
                    }

                    // 🎯 Add hover effect
                    button.MouseEnter += (s, e) =>
                    {
                        if (button.Name.ToLower().Contains("submit") || button.Name.ToLower().Contains("editexistingsystem"))
                        {
                            button.BackColor = Color.OrangeRed;
                        }
                        else
                        {
                            button.BackColor = Color.FromArgb(30, 30, 60); // Slightly lighter blue hover
                        }
                    };
                    button.MouseLeave += (s, e) =>
                    {
                        if (button.Name.ToLower().Contains("submit") || button.Name.ToLower().Contains("editexistingsystem"))
                        {
                            button.BackColor = Color.FromArgb(255, 140, 0);
                        }
                        else
                        {
                            button.BackColor = Color.FromArgb(20, 20, 40);
                        }
                    };
                }
                else if (control is ComboBox comboBox)
                {
                    comboBox.FlatStyle = FlatStyle.Flat;
                    comboBox.BackColor = Color.FromArgb(20, 20, 40);
                    comboBox.ForeColor = Color.White;
                }
                else if (control is TextBox textBox)
                {
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    textBox.BackColor = Color.FromArgb(20, 20, 40);
                    textBox.ForeColor = Color.White;
                }
                else if (control is Panel panel)
                {
                    string panelName = panel.Name.ToLower();

                    if (panelName.Contains("edit"))
                    {
                        panel.BackColor = Color.FromArgb(20, 20, 50); // Dark blue for Edit panels
                    }
                    else if (panelName.Contains("create") ||
                             panelName.Contains("commander") ||
                             panelName.Contains("systemandstationloading") ||
                             panelName.Contains("liveterminal") ||
                             panelName.Contains("unloadcargo"))
                    {
                        panel.BackColor = Color.FromArgb(40, 20, 0); // Dark orange for commander+top+terminal panels
                    }
                    else
                    {
                        panel.BackColor = Color.FromArgb(10, 10, 10); // Default super dark for everything else
                    }
                }

                // 🚀 Recurse into child controls
                if (control.HasChildren)
                {
                    ApplyCustomStyle(control);
                }
            }
        }

    }
}

