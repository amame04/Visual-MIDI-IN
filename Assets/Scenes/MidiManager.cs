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
        if (Input.GetKey(KeyCode.Escape))
        {
            SetPanelActive();
        }
        for (int i = 0; i < MidiOnOff.Length; i++)
        {
            if(MidiOnOff[i] == true)
            {
                if (MidiVelocityArray[i] != 0)
                NoteOn(i, MidiVelocityArray[i]);
            }
            else
            {
                NoteOff(i);
            }
        }
    }
    private void OnEnable()
    {
        StartMidiInPort(0);
        StartMidiOutPort(0);
        SetPanelActive();
    }

    private void OnDisable()
    {
        StopMidiInPort();
        StopMidiOutPort();
    }

    private void StartMidiInPort(int DevNumber)
    {
        MidiInPort = new MidiIn(DevNumber);
        MidiInPort.MessageReceived += MidiInMessageReceived;
        MidiInPort.ErrorReceived += MidiInErrorReceived;
        MidiInPort.Start();
    }
    
    private void StartMidiOutPort(int DevNumber)
    {
        MidiOutPort = new MidiOut(DevNumber);
    }
    
    private void StopMidiInPort()
    {
        MidiInPort.Stop();
        MidiInPort.Dispose();
        MidiInPort.MessageReceived -= MidiInMessageReceived;
        MidiInPort.ErrorReceived -= MidiInErrorReceived;
    }
    
    private void StopMidiOutPort()
    {
        MidiOutPort.Dispose();
    }

    void MidiInErrorReceived(object sender, MidiInMessageEventArgs e)
    {
        Debug.Log("Error Received");
    }

    void MidiInMessageReceived(object sender, MidiInMessageEventArgs e)
    {
        MidiOutPort.Send(e.RawMessage);

        int MidiVelocity = (e.RawMessage & 0xff0000) >> 16;
        int MidiNote = (e.RawMessage & 0xff00) >> 8;
        int MidiEvent = e.RawMessage & 0x000000ff;

        if (MidiEvent == 0x90 && MidiVelocity != 0)
        {
            MidiOnOff[MidiNote] = true;
            MidiVelocityArray[MidiNote] = MidiVelocity;
        } else if ((MidiEvent == 0x90 && MidiVelocity == 0) || (MidiEvent == 0x80))
        {
            MidiOnOff[MidiNote] = false;
        }
    }

    private void NoteOn(int MidiNote, int Velocity)
    {
        string NoteName = ReturnNoteName(MidiNote);
        GameObject CurNote = GameObject.Find(NoteName);
        CurNote.GetComponent<Animator>().SetBool("midiOn", true);

        PlayFireWorks(MidiNote, Velocity);
    }
    
    private void NoteOff(int MidiNote)
    {
        string NoteName = ReturnNoteName(MidiNote);
        GameObject CurNote = GameObject.Find(NoteName);
        CurNote.GetComponent<Animator>().SetBool("midiOn", false);
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
        FireWorksParTr.colorOverTrail = FireWorksGrad;
        
        if (!FireWorksPar.isPlaying)
        {
        float FireWorksScale = Velocity / 7.0f;
        FireWorksObj.transform.localScale = new Vector3(FireWorksScale, FireWorksScale, FireWorksScale);
        FireWorksPar.Play();
        }
        MidiVelocityArray[MidiNote] = 0;
    }

    public void SetPanelActive()
    {
        SetPanel.SetActive(true);
        ListingMidiDev();

        MidiInDrp.value = CurMidiDev[0];
        MidiOutDrp.value = CurMidiDev[1];
    }

    public void PushOkBtn()
    {
        StopMidiInPort();
        StopMidiOutPort();
        StartMidiInPort(MidiInDrp.value);
        StartMidiOutPort(MidiOutDrp.value);

        CurMidiDev[0] = MidiInDrp.value;
        CurMidiDev[1] = MidiOutDrp.value;

        SetPanel.SetActive(false);
    }

    public void PushCancelBtn()
    {
        SetPanel.SetActive(false);
    }

    public void ListingMidiDev()
    {
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

        MidiInDrp.ClearOptions();
        MidiOutDrp.ClearOptions();

        MidiInDrp.AddOptions(MidiInList);
        MidiOutDrp.AddOptions(MidiOutList);
    }
}