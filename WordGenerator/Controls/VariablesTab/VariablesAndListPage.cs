using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using DataStructures;

namespace WordGenerator.Controls
{
    public partial class VariablesAndListPage : UserControl
    {

        private List<VariableEditor> variableEditors;
        private List<ListEditorPanel> listPanels;

        public enum VariablesOrderingMethods {[Description("Alphabetically")] Alphabetical, [Description("By Creation Time")] Creation };

        public VariablesAndListPage()
        {
            InitializeComponent();
            
            this.runControl1.runZeroButton.Visible = false;
            this.runControl1.repeatCheckBox.Visible = false;
            this.runControl1.runListButton.Visible = false;
            this.runControl1.RunNoSave.Visible = false;
            this.runControl1.runRandomList.Visible = false;
            this.runControl1.continueListButton.Visible = false;
            
            variableEditors = new List<VariableEditor>();
            listPanels = new List<ListEditorPanel>();
            for (int i = 0; i < ListData.NLists; i++)
            {
                ListEditorPanel lep = new ListEditorPanel();
                lep.setListID(i + 1);
                int x = listEditorPanelPlaceholder.Location.X + i * listEditorPanelPlaceholder.Width;
                int y = listEditorPanelPlaceholder.Location.Y;
                lep.Location = new Point(x, y);
                listPanels.Add(lep);
            }
            this.Controls.AddRange(listPanels.ToArray());
            this.runControl1.SendToBack();
            this.listFillerSelector.Items.Clear();
            for (int i = 0; i < ListData.NLists; i++)
            {
                this.listFillerSelector.Items.Add("List " + (i + 1));
            }
            if (!Storage.sequenceData.OrderingGroups.ContainsKey(SequenceData.OrderingGroupTypes.Variables))
                Storage.sequenceData.OrderingGroups.Add(SequenceData.OrderingGroupTypes.Variables, new HashSet<string>());
            populateSortMethods();
            sortMethods.SelectedItem = VariablesOrderingMethods.Alphabetical.GetDescription();
        }

        public void layout()
        {
            if (MainClientForm.instance != null)
                MainClientForm.instance.cursorWait();
            try
            {
                for (int i = 0; i < ListData.NLists; i++)
                {
                    listPanels[i].setListID(i + 1);
                }



                for (int i = 0; i < ListData.NLists; i++)
                {
                    listPanels[i].setData(Storage.sequenceData.Lists.Lists[i]);

                }


                if (!Storage.sequenceData.Lists.ListLocked)
                    unlockLists();
                else
                    lockLists();

                this.layoutVariables();

                layoutCalibrationUI();

            }
            finally
            {
                if (MainClientForm.instance != null)
                    MainClientForm.instance.cursorWaitRelease();
            }
        }

        private void layoutCalibrationUI()
        {
            if (Storage.sequenceData != null)
            {
                if (Storage.sequenceData.CalibrationShotsInfo.CalibrationShotSequence == null)
                {
                    calShotSequenceLabel.Text = "None loaded.";
                }
                else
                {
                    if (Storage.sequenceData.CalibrationShotsInfo.CalibrationShotSequence.SequenceName == null ||
                        Storage.sequenceData.CalibrationShotsInfo.CalibrationShotSequence.SequenceName == "")
                    {
                        calShotSequenceLabel.Text = "Unnamed sequence.";
                    }
                    else
                    {
                        calShotSequenceLabel.Text = Storage.sequenceData.CalibrationShotsInfo.CalibrationShotSequence.SequenceName;
                    }
                }

                calibEnabled.Checked = Storage.sequenceData.CalibrationShotsInfo.CalibrationShotsEnabled;
                runCalFirstCheck.Checked = Storage.sequenceData.CalibrationShotsInfo.RunCalibrationShotFirst;
                runCalLastCheck.Checked = Storage.sequenceData.CalibrationShotsInfo.RunCalibrationShotLast;
                runEveryNCheck.Checked = Storage.sequenceData.CalibrationShotsInfo.RunCalibrationShotEveryN;
                runCalN.Value = Storage.sequenceData.CalibrationShotsInfo.RunCalibrationShotN;
                enableOrDisableCalibrationShotsUI();
            }
        }

        private void layoutVariables()
        {
            this.variablesPanel.SuspendLayout();

            if (sortVariables.Checked)
                OrderVariableEditors(sortMethods.SelectedItem as String);

            //Remove all the ordering group labels
            foreach (Control con in variablesPanel.Controls)
            {
                if (con is Label && con.Name.EndsWith("group box"))
                    variablesPanel.Controls.Remove(con);
            }

            if (Storage.sequenceData == null || Storage.sequenceData.Variables == null)
                discardAndRefreshAllVariableEditors();
            else
            {
                //Count how many variable editors need to be shown
                int shownVariables = 0;
                foreach (Variable var in Storage.sequenceData.Variables)
                {
                    if (!var.IsSpecialVariable)
                        shownVariables++;
                }

                // if more than we currently have, add some
                if (shownVariables > variableEditors.Count)
                {
                    int extras = shownVariables - variableEditors.Count;
                    for (int i = 0; i < extras; i++)
                    {
                        variablesPanel.Controls.Add(createAndRegisterNewVariableEditor(null));
                    }
                }
                // if less than we currently have, remove some
                else if (shownVariables < variableEditors.Count)
                {
                    int extras = variableEditors.Count - shownVariables;
                    for (int i = 0; i < extras; i++)
                    {
                        variablesPanel.Controls.Remove(variableEditors[0]);
                        variableEditors[0].Dispose();
                        variableEditors.RemoveAt(0);
                    }
                }

                // Now we have the correct number of variable editors, let's update them to point at
                // the correct variables
                int j = 0;
                foreach (Variable var in Storage.sequenceData.Variables)
                {
                    if (!var.IsSpecialVariable)
                    {
                        variableEditors[j].setVariable(var);
                        j++;
                    }
                }
            }

            if (orderByGroup.Checked && variableEditors.Count > 0)
                addGroupLabels();

            this.variablesPanel.ResumeLayout();

        }

        /// <summary>
        /// Somewhat slow but guaranteed to work layout of variable editors.
        /// </summary>
        private void discardAndRefreshAllVariableEditors()
        {
            foreach (VariableEditor ved in variableEditors)
            {
                this.variablesPanel.Controls.Remove(ved);
                ved.Dispose();
            }

            variableEditors.Clear();

            if (Storage.sequenceData != null && Storage.sequenceData.Variables != null)
            {
                foreach (Variable var in Storage.sequenceData.Variables)
                {
                    if (!var.IsSpecialVariable)
                    {
                        createAndRegisterNewVariableEditor(var);
                    }
                }


                this.variablesPanel.Controls.AddRange(variableEditors.ToArray());
            }
        }

        private VariableEditor createAndRegisterNewVariableEditor(Variable var)
        {
            VariableEditor ved = new VariableEditor();
            ved.setVariable(var);
            /*        int x = variableEditorPlaceholder.Location.X;
                    int y = variableEditorPlaceholder.Location.Y + variableEditors.Count * (variableEditorPlaceholder.Height + 5);
                    ved.Location = new Point(x, y);*/
            ved.variableDeleted += new EventHandler(ved_variableDeleted);
            //      ved.SizeChanged += new EventHandler(ved_SizeChanged);
            ved.valueChanged += new EventHandler(ved_valueChanged);
            variableEditors.Add(ved);
            return ved;
        }

        public void ved_valueChanged(object sender, EventArgs e)
        {
            foreach (VariableEditor ved in variableEditors)
            {
                ved.updateDerivedValue();
            }
        }

        void ved_variableDeleted(object sender, EventArgs e)
        {
            if (sender is VariableEditor)
            {
                VariableEditor ved = (VariableEditor)sender;
                // faster way to delete this variable from UI,
                // if we know which specific editor it was
                variableEditors.Remove(ved);
                variablesPanel.Controls.Remove(ved);
                ved.Dispose();

            }
            else
            {
                this.layout();
            }
        }

        /// <summary>
        /// Adds ordering group labels based on the editors in the current variables panel.
        /// </summary>
        private void addGroupLabels()
        {
            int spacing = 3;
            int extraSpacing = 10; //Pixels between each group label and the previous editor
            int inserted = 0; //Counts the number of group labels added so far
            Label groupNameBox;
            String lastGroup = variableEditors[0].getVariable().OrderingGroup + 'a';
            for (int i = 0; i < variableEditors.Count; i++)
            {
                if (variableEditors[i].getVariable().OrderingGroup != lastGroup 
                    && variableEditors[i].getVariable().OrderingGroup != null)
                {
                    groupNameBox = new Label();
                    groupNameBox.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold);
                    groupNameBox.Name = variableEditors[i].getVariable().OrderingGroup + " group box";
                    groupNameBox.Size = new Size(variableEditors[i].Width, 17);
                    groupNameBox.Text = variableEditors[i].getVariable().OrderingGroup;
                    groupNameBox.Margin = new Padding(spacing, spacing + extraSpacing, spacing, spacing);
                    variablesPanel.Controls.Add(groupNameBox);
                    variablesPanel.Controls.SetChildIndex(groupNameBox, i + 2 + inserted);
                    inserted++;
                }

                lastGroup = variableEditors[i].getVariable().OrderingGroup;
            }
        }


        private void addButton_Click(object sender, EventArgs e)
        {
            int id = Storage.sequenceData.GenerateNewVariableID();
            Variable var = new Variable(id);
            var.VariableName = "Var" + id;
            Storage.sequenceData.Variables.Add(var);
            this.layoutVariables();
        }

        public void tryLockLists()
        {
            if (!Storage.sequenceData.Lists.ListLocked)
            {
                this.Invoke(new EventHandler(lockButton_Click), new object[] { null, null });
            }
        }

        private void lockButton_Click(object sender, EventArgs e)
        {
            this.LockMessage.Text = "";

            if (Storage.sequenceData==null)
                return;
            if (Storage.sequenceData.Lists==null) 
                return;

            if (!Storage.sequenceData.Lists.ListLocked)
            {

                // check for line readability
                for (int i = 0; i < ListData.NLists; i++)
                {
                    if (Storage.sequenceData.Lists.ListEnabled[i])
                    {
                        if (!listPanels[i].isListParsable())
                        {
                            this.LockMessage.Text = "List " + (i + 1) + " contains unreadable lines.";
                            return;
                        }
                    }
                }

                // check to make sure that no variable is assigned to a disabled list
                List<int> usedList = new List<int>();
                foreach (Variable var in Storage.sequenceData.Variables)
                {
                    if (var.ListDriven && !Storage.sequenceData.Lists.ListEnabled[var.ListNumber - 1])
                    {
                        this.LockMessage.Text = "Variable [" + var.ToString() + "] is bound to disabled List " + (var.ListNumber) + ".";
                        return;
                    }
                    else if (var.ListDriven)
                        usedList.Add(var.ListNumber - 1);
                }
                // disable all unneccessarily enabled list
                for (int i = 0; i < ListData.NLists - 1; i++)
                {
                    if (!usedList.Contains(i) && Storage.sequenceData.Lists.ListEnabled[i])
                    listPanels[i].setEnabledBox(false);  
                }
                // check for length matching
                for (int i = 0; i < ListData.NLists - 1; i++)
                {
                    if (Storage.sequenceData.Lists.ListEnabled[i])
                    {
                        if (Storage.sequenceData.Lists.Cross[i])
                            continue;
                        for (int j = i + 1; j < ListData.NLists; j++)
                        {
                            if (Storage.sequenceData.Lists.ListEnabled[j])
                            {
                                if (listPanels[i].NLines() == listPanels[j].NLines())
                                    break;
                                else
                                {
                                    this.LockMessage.Text = "Lists " + (i + 1) + " and " + (j + 1) + " have different lengths.";
                                    return;
                                }
                            }
                        }
                    }
                }


                // Tests passed. Ok, lets lock the lists.

                lockLists();

                Storage.sequenceData.ListIterationNumber = 0;

                ved_valueChanged(this, null);
            }
            else // unlock lists
            {
                unlockLists();
            }



        }

        private void lockLists()
        {
            foreach (ListEditorPanel pan in listPanels)
            {
                pan.Enabled = false;
            }
            Storage.sequenceData.Lists.ListLocked = true;

            //variablesPanel.Enabled = false;


            foreach (VariableEditor ve in variableEditors)
            {
                ve.ListLocked = true;
            }


            addButton.Enabled = false;

            for (int i = 0; i < ListData.NLists; i++)
            {
                Storage.sequenceData.Lists.Lists[i] = listPanels[i].parseList();
            }

            this.lockButton.Text = Storage.sequenceData.Lists.iterationsCount() + " iterations.\r\nUnlock Lists.";

        }

        private void unlockLists()
        {
            Storage.sequenceData.Lists.ListLocked = false;
            this.lockButton.Text = "Lock Lists.";
            foreach (ListEditorPanel pan in listPanels)
            {
                pan.Enabled = true;
            }

           // variablesPanel.Enabled = true;

            foreach (VariableEditor ve in variableEditors)
            {
                ve.ListLocked = false;
            }

            addButton.Enabled = true;
        }

        private void calibEnabled_CheckedChanged(object sender, EventArgs e)
        {
            if (Storage.sequenceData != null)
            {
                Storage.sequenceData.calibrationShotsInfo.CalibrationShotsEnabled = calibEnabled.Checked;
            }

            enableOrDisableCalibrationShotsUI();
        }

        private void enableOrDisableCalibrationShotsUI()
        {
            if (calibEnabled.Checked == false || Storage.sequenceData == null)
            {
                runCalFirstCheck.Enabled = false;
                runCalLastCheck.Enabled = false;
                runCalN.Enabled = false;
                runEveryNCheck.Enabled = false;
                runEveryNCheck.Enabled = false;
                calShotSequenceLabel.Enabled = false;
                calShotSeqLabInfo.Enabled = false;
                loadCalSequenceFromFile.Enabled = false;
                loadCalSequenceFromCurrentSequence.Enabled = false;
                unloadCalSequence.Enabled = false;
            }
            else
            {
                runCalFirstCheck.Enabled = true;
                runCalLastCheck.Enabled = true;
                runCalN.Enabled = Storage.sequenceData.CalibrationShotsInfo.RunCalibrationShotEveryN;
                runEveryLabel.Enabled = Storage.sequenceData.CalibrationShotsInfo.RunCalibrationShotEveryN;
                runEveryNCheck.Enabled = true;
                calShotSequenceLabel.Enabled = true;
                calShotSeqLabInfo.Enabled = true;
                if (Storage.sequenceData.CalibrationShotsInfo.CalibrationShotSequence == null)
                {
                    loadCalSequenceFromFile.Enabled = true;
                    loadCalSequenceFromCurrentSequence.Enabled = true;
                    unloadCalSequence.Enabled = false;
                }
                else
                {
                    unloadCalSequence.Enabled = true;
                    loadCalSequenceFromFile.Enabled = false;
                    loadCalSequenceFromCurrentSequence.Enabled = false;
                }
            }
        }

        private void runCalFirstCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (Storage.sequenceData != null)
                Storage.sequenceData.calibrationShotsInfo.RunCalibrationShotFirst = runCalFirstCheck.Checked;
        }

        private void runCalLastCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (Storage.sequenceData != null)
                Storage.sequenceData.calibrationShotsInfo.RunCalibrationShotLast = runCalLastCheck.Checked;
        }

        private void runEveryNCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (Storage.sequenceData != null)
                Storage.sequenceData.calibrationShotsInfo.RunCalibrationShotEveryN = runEveryNCheck.Checked;
            enableOrDisableCalibrationShotsUI();
        }

        private void runCalN_ValueChanged(object sender, EventArgs e)
        {
            if (Storage.sequenceData != null)
                Storage.sequenceData.calibrationShotsInfo.RunCalibrationShotN = (int)runCalN.Value;

        }

        private void unloadCalSequence_Click(object sender, EventArgs e)
        {
            if (Storage.sequenceData != null)
            {
                Storage.sequenceData.calibrationShotsInfo.CalibrationShotSequence = null;
                layoutCalibrationUI();
            }
        }

        private void loadCalSequence_Click(object sender, EventArgs e)
        {
            if (Storage.sequenceData != null)
            {
                SequenceData calibrationShotSequence = Storage.SaveAndLoad.LoadSequenceWithFileDialog();

                setCalibrationSequence(calibrationShotSequence);
            }
        }

        private void setCalibrationSequence(SequenceData calibrationShotSequence)
        {
            Storage.sequenceData.calibrationShotsInfo.CalibrationShotSequence = calibrationShotSequence;
            if (Storage.sequenceData.calibrationShotsInfo.CalibrationShotSequence != null)
            {
                if (!Storage.sequenceData.calibrationShotsInfo.CalibrationShotSequence.Lists.ListLocked)
                {
                    MessageBox.Show("The sequence you have specified for use as a calibration shot does not have its lists locked, and therefore will not be usable as a calibration shot. To use this file as a calibration shot, lock its lists.", "Unable to use calibration shot with unlocked lists.");
                    Storage.sequenceData.calibrationShotsInfo.CalibrationShotSequence = null;

                }
                else
                    Storage.sequenceData.calibrationShotsInfo.CalibrationShotSequence.CalibrationShot = true;
            }
            layoutCalibrationUI();
        }

        private void equationHelpButton_Click(object sender, EventArgs e)
        {
            FormulaHelpDialog help = new FormulaHelpDialog();
            help.ShowDialog();
        }

        private void permanentVariablesButton_Click(object sender, EventArgs e)
        {
            PermanentVariablesEditor pve = new PermanentVariablesEditor();
            pve.ShowDialog();
            foreach (VariableEditor ve in this.variableEditors)
            {
                ve.updateLayout();
            }
        }

        private void listFillerSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.listFillerButton.Enabled = Storage.sequenceData.Lists.ListEnabled[listFillerSelector.SelectedIndex] && !Storage.sequenceData.Lists.ListLocked;
        }

        private void listFillerButton_Click(object sender, EventArgs e)
        {
            List<Double> fillers = new List<double>();
            try
            {
                int numOfFillers = (int)(1 + (this.listFillerStop.Value - this.listFillerStart.Value) / this.listFillerStep.Value);
                for (int i = 0; i < numOfFillers; i++)
                {
                    fillers.Add((double)(this.listFillerStart.Value + i * this.listFillerStep.Value));
                }
                listPanels[listFillerSelector.SelectedIndex].setData(fillers);
            }
            catch
            {
                MessageBox.Show("Check your parameters.");
            }
         }

        private void loadCalSequenceFromCurrentSequence_Click(object sender, EventArgs e)
        {
            SequenceData copyOfCurrentSequence = (SequenceData)Common.createDeepCopyBySerialization(Storage.sequenceData);
            setCalibrationSequence(copyOfCurrentSequence);
        }

        private void populateSortMethods()
        {
            sortMethods.Items.Clear();
            Array methods = Enum.GetValues(typeof(VariablesOrderingMethods));
            foreach (VariablesOrderingMethods method in methods)
                sortMethods.Items.Add(method.GetDescription());
        }

        private void sortMethods_DropDown(object sender, EventArgs e)
        {
            populateSortMethods();
        }

        private void sortMethods_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sortVariables.Checked)
                layoutVariables();
        }

        private void sortVariables_CheckedChanged(object sender, EventArgs e)
        {
            if (sortVariables.Checked)
                layoutVariables();
        }

        /// <summary>
        /// Sorts the list Storage.sequenceData.Variables according to the input method.
        /// </summary>
        /// <param name="method">VariablesOrderingMethods element to use as the sort method.</param>
        private void OrderVariableEditors(String method)
        {
            if (method == VariablesOrderingMethods.Alphabetical.GetDescription())
                Storage.sequenceData.Variables.Sort((x, y) => x.CompareByAlphabeticalPosition(y, orderByGroup.Checked));
            else if (method == VariablesOrderingMethods.Creation.GetDescription())
                Storage.sequenceData.Variables.Sort((x, y) => x.CompareByVariableID(y, orderByGroup.Checked));
        }

        private void deleteOrderingGroupButton_Click(object sender, EventArgs e)
        {
            String groupToDelete = orderingGroupComboBox.SelectedItem as String;
            Storage.sequenceData.OrderingGroups[SequenceData.OrderingGroupTypes.Variables].Remove(groupToDelete);
            //For each pulse that used to be in this ordering group, 
            //set its new ordering group to be null
            foreach (Variable variable in Storage.sequenceData.Variables)
            {
                if (variable.OrderingGroup == groupToDelete)
                    variable.OrderingGroup = null;
            }

            orderingGroupComboBox.SelectedItem = null;
            layout();
        }

        private void createOrderingGroupButton_Click(object sender, EventArgs e)
        {
            Storage.sequenceData.OrderingGroups[SequenceData.OrderingGroupTypes.Variables].Add(orderingGroupTextBox.Text);
        }

        private void populateOrderingGroupComboBox()
        {
            if (Storage.sequenceData != null)
            {
                orderingGroupComboBox.Items.Clear();
                List<String> sortedGroups = new List<String>(Storage.sequenceData.OrderingGroups[SequenceData.OrderingGroupTypes.Variables]);
                sortedGroups.Sort();
                foreach (String group in sortedGroups)
                    orderingGroupComboBox.Items.Add(group);
            }
        }

        private void orderingGroupComboBox_DropDown(object sender, EventArgs e)
        {
            populateOrderingGroupComboBox();
        }

        private void orderByGroup_CheckedChanged(object sender, EventArgs e)
        {
            if (sortVariables.Checked)
                layoutVariables();
        }
    }
}
