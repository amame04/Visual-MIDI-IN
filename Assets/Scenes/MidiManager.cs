using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NAudio.Midi;

public class MidiManager : MonoBehaviour
{
    [SerializeField] GameObject SetPanel;
    [SerializeField] Dropdown MidiInDrp;
    [SerializeField] Dropdown MidiOutDrp;

    MidiIn MidiInPort;
    MidiOut MidiOutPort;

    bool[] MidiOnOff = new bool[132];
    int[] MidiVelocityArray = new int[132];

    int[] CurMidiDev = { 0, 0 };

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // If you press Esc, it show the menu
        if (Input.GetKey(KeyCode.Escape))
        {
            ShowSettingMenu();
        }

        // Monitor changes of MidiOnOff and MidiVelocityArray
        for (int i = 0; i < MidiOnOff.Length; i++)
        {
            if(MidiOnOff[i] == true)
            {
                if (MidiVelocityArray[i] != 0)
                {
                    // Start animation
                    NoteOn(i, MidiVelocityArray[i]);
                }
            }
            else
            {
                // Stop animation
                NoteOff(i);
            }
        }
    }
    private void OnEnable()
    {
        // Start MIDI port
        StartMidiInPort(0);
        StartMidiOutPort(0);

        // Show menu
        ShowSettingMenu();
    }

    private void OnDisable()
    {
        // Stop MIDI port
        StopMidiInPort();
        StopMidiOutPort();
    }

    // Lock MIDI input device
    private void StartMidiInPort(int DevNumber)
    {
        MidiInPort = new MidiIn(DevNumber);
        MidiInPort.MessageReceived += MidiInMessageReceived;
        MidiInPort.ErrorReceived += MidiInErrorReceived;
        MidiInPort.Start();
    }
    
    // Lock MIDI output device
    private void StartMidiOutPort(int DevNumber)
    {
        MidiOutPort = new MidiOut(DevNumber);
    }
    
    // Release MIDI input device
    private void StopMidiInPort()
    {
        MidiInPort.Stop();
        MidiInPort.Dispose();
        MidiInPort.MessageReceived -= MidiInMessageReceived;
        MidiInPort.ErrorReceived -= MidiInErrorReceived;
    }
    
    // Release MIDI output device
    private void StopMidiOutPort()
    {
        MidiOutPort.Dispose();
    }

    // If it received MIDI input error, it output logs (Finally, it do nothing)
    void MidiInErrorReceived(object sender, MidiInMessageEventArgs e)
    {
        Debug.Log("Error Received");
    }

    // If it recieved MIDI message, it send Raw message to MIDI output and Start animation
    void MidiInMessageReceived(object sender, MidiInMessageEventArgs e)
    {
        MidiOutPort.Send(e.RawMessage);

        int MidiVelocity = (e.RawMessage & 0xff0000) >> 16;
        int MidiNote = (e.RawMessage & 0xff00) >> 8;
        int MidiEvent = e.RawMessage & 0x000000ff;

        // If MidiEvent == Note on
        if (MidiEvent == 0x90 && MidiVelocity != 0)
        {
            MidiOnOff[MidiNote] = true;
            MidiVelocityArray[MidiNote] = MidiVelocity;
        }
        // If MidiEvent == Note Off
        else if ((MidiEvent == 0x90 && MidiVelocity == 0) || (MidiEvent == 0x80))
        {
            MidiOnOff[MidiNote] = false;
        }
    }

    // Change key color and play animation
    private void NoteOn(int MidiNote, int Velocity)
    {
        string NoteName = ReturnNoteName(MidiNote);
        GameObject CurNote = GameObject.Find(NoteName);
        //CurNote.GetComponent<Animator>().SetBool("midiOn", true);

        CurNote.GetComponent<Renderer>().material.color = Color.green;

        PlayFireWorks(MidiNote, Velocity);
    }
    
    // Revert key color
    private void NoteOff(int MidiNote)
    {
        string NoteName = ReturnNoteName(MidiNote);
        GameObject CurNote = GameObject.Find(NoteName);
        //CurNote.GetComponent<Animator>().SetBool("midiOn", false);
        
        switch (MidiNote % 12)
        {
            case 1:
            case 3:
            case 6:
            case 8:
            case 10:
                CurNote.GetComponent<Renderer>().material.color = Color.black;
                break;
            default:
                CurNote.GetComponent<Renderer>().material.color = Color.white;
                break;
        }
    }

    private string ReturnNoteName (int MidiNote)
    {
        int Octave = MidiNote / 12;
        string[] NoteNameArray = {"C", "Cis", "D", "Dis", "E", "F", "Fis", "G", "Gis", "A", "Ais", "H"};
        return  NoteNameArray[MidiNote % 12] + Octave;
    }

    private void PlayFireWorks(int MidiNote, int Velocity)
    {
        var FireWorksObj = GameObject.Find("Fireworks" + MidiNote);
        var FireWorksPar = FireWorksObj.GetComponent<ParticleSystem>();
        var FireWorksParTr = FireWorksPar.trails;

        // Create ramdom gradation
        var FireWorksGrad = new Gradient();

        var FireWorksGradCol = new GradientColorKey[2];
        FireWorksGradCol[0].color = new Color(Random.value, Random.value, Random.value);
        FireWorksGradCol[0].time = 0.0f;
        FireWorksGradCol[1].color = new Color(Random.value, Random.value, Random.value);
        FireWorksGradCol[1].time = 0.8f;

        var FireWorksGradAlf = new GradientAlphaKey[2];
        FireWorksGradAlf[0].alpha = 1.0f;
        FireWorksGradAlf[0].time = 0.7f;
        FireWorksGradAlf[1].alpha = 0.0f;
        FireWorksGradAlf[1].time = 1.0f;

        FireWorksGrad.SetKeys(FireWorksGradCol, FireWorksGradAlf);

        // Apply the gradation
        FireWorksParTr.colorOverTrail = FireWorksGrad;
        
        // Play FireWorks
        if (!FireWorksPar.isPlaying)
        {
        float FireWorksScale = Velocity / 7.0f;
        FireWorksObj.transform.localScale = new Vector3(FireWorksScale, FireWorksScale, FireWorksScale);
        FireWorksPar.Play();
        }
        MidiVelocityArray[MidiNote] = 0;
    }

    public void ShowSettingMenu()
    {
        SetPanel.SetActive(true);

        // Enumelate midi device
        ListingMidiDev();

        // Set cursor the device that you're using
        MidiInDrp.value = CurMidiDev[0];
        MidiOutDrp.value = CurMidiDev[1];
    }

    // Apply changes and hide menu
    public void PushOkBtn()
    {
        StopMidiInPort();
        StopMidiOutPort();
        StartMidiInPort(MidiInDrp.value);
        StartMidiOutPort(MidiOutDrp.value);

        // Change current midi device
        CurMidiDev[0] = MidiInDrp.value;
        CurMidiDev[1] = MidiOutDrp.value;

        SetPanel.SetActive(false);
    }

    // Hide menu (do nothing)
    public void PushCancelBtn()
    {
        SetPanel.SetActive(false);
    }

    // Enumelate and list available midi devices
    public void ListingMidiDev()
    {
        // List midi devices
        var MidiInList = new List<string>();
        var MidiOutList = new List<string>();

        for (int device = 0; device < MidiIn.NumberOfDevices; device++)
        {
            MidiInList.Add(MidiIn.DeviceInfo(device).ProductName);
        }

        for (int device = 0; device < MidiOut.NumberOfDevices; device++)
        {
            MidiOutList.Add(MidiOut.DeviceInfo(device).ProductName);
        }

        // Clear lsit
        MidiInDrp.ClearOptions();
        MidiOutDrp.ClearOptions();

        // Set list
        MidiInDrp.AddOptions(MidiInList);
        MidiOutDrp.AddOptions(MidiOutList);
    }
}