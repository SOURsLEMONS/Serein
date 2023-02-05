﻿using NCrontab;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace Serein.Ui.ChildrenWindow
{
    public partial class TaskEditor : Form
    {
        public bool CancelFlag { get; private set; } = true;
        public TaskEditor()
        {
            InitializeComponent();
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Confirm_Click(object sender, EventArgs e)
        {
            if (!(string.IsNullOrEmpty(Cron.Text) || string.IsNullOrWhiteSpace(Cron.Text) ||
                string.IsNullOrEmpty(Command.Text) || string.IsNullOrWhiteSpace(Command.Text)))
            {
                if (Core.Command.GetType(Command.Text) == Base.CommandType.Invalid)
                {
                    MessageBox.Show("执行命令无效", "Serein", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                try
                {
                    List<DateTime> Occurrences = CrontabSchedule.Parse(Cron.Text).GetNextOccurrences(DateTime.Now, DateTime.Now.AddYears(1)).ToList();
                    CronNextTime.Text = "下一次执行时间:" + Occurrences[0].ToString();
                    CancelFlag = false;
                    Close();
                }
                catch
                {
                    CronNextTime.Text = "Cron表达式无效";
                    MessageBox.Show("Cron表达式无效", "Serein", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show("内容为空", "Serein", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Cron_TextChanged(object sender, EventArgs e)
        {
            try
            {
                List<DateTime> Occurrences = CrontabSchedule.Parse(Cron.Text).GetNextOccurrences(DateTime.Now, DateTime.Now.AddYears(1)).ToList();
                CronNextTime.Text = "下一次执行时间:" + Occurrences[0].ToString();
            }
            catch
            {
                CronNextTime.Text = "Cron表达式无效";
            }
        }
        public void Update(string CronText, string RemarkText, string CommandText)
        {
            Cron.Text = CronText;
            Command.Text = CommandText;
            Remark.Text = RemarkText;
        }

        private void Cron_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Command.Focus();
            }
        }

        private void Remark_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Confirm.Focus();
                Confirm_Click(this, EventArgs.Empty);
            }
        }

        private void Command_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Remark.Focus();
            }
        }
        private void Cron_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(13))
            {
                e.Handled = true;
            }
        }
        private void Command_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(13))
            {
                e.Handled = true;
            }
        }
        private void Remark_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(13))
            {
                e.Handled = true;
            }
        }

        private void TaskEditer_HelpButtonClicked(object sender, CancelEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://serein.cc/#/Function/Task") { UseShellExecute = true });
        }
    }
}
