using PetTracker.Core;
using PetTracker.Capture;
using System.Collections.Concurrent;

namespace PetTracker.App;

public sealed class TrackerForm : Form
{
    private readonly TextBox character = new() { Text = "Demo Karakter", Width = 155 };
    private readonly TextBox server = new() { Text = "Demo Global", Width = 140 };
    private readonly ComboBox pets = new() { Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown petId = new() { Minimum = 1, Maximum = int.MaxValue, Value = 1002, Width = 90 };
    private readonly NumericUpDown amount = new() { Minimum = 1, Maximum = 999, Value = 1, Width = 70 };
    private readonly Label status = new() { AutoSize = true, Padding = new(8) };
    private readonly DataGridView grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false };
    private readonly TextBox debug = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
    private readonly PetCatalog catalog;
    private readonly Tracker tracker;
    private readonly StateStore store;
    private readonly string logPath;
    private string stream = Guid.NewGuid().ToString("N");
    private long frame;
    private readonly bool liveMode;
    private readonly ConcurrentQueue<LiveMessage> messages = new();
    private readonly System.Windows.Forms.Timer poll = new() { Interval = 250 };
    private readonly LiveSession liveSession;
    private PassiveCapture? capture;
    private int overflow;
    private bool closing;
    private bool allowClose;
    private Task? stopping;
    private Button? startButton, stopButton;

    public TrackerForm(bool liveMode = true, string? stateFolder = null)
    {
        this.liveMode = liveMode;
        Text = liveMode ? "Aion 2 Pet Tracker — CANLI / PASİF" : "Aion 2 Pet Tracker — DEMO / ÇEVRİMDIŞI";
        MinimumSize = new(1000, 650); Size = new(1120, 750);
        var folder = stateFolder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aion2PetTracker", liveMode ? "Live" : "Demo");
        Directory.CreateDirectory(folder); logPath = Path.Combine(folder, "tracker.log");
        catalog = PetCatalog.Load(Path.Combine(AppContext.BaseDirectory, "data", "pet-catalog.json"));
        store = new(Path.Combine(folder, "state.json"), Log);
        tracker = new(catalog, store.Load(), Log);
        liveSession = new(tracker, Log, catalog);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6 };
        foreach (var height in new[] { 48, 48, 48, 48 }) layout.RowStyles.Add(new(SizeType.Absolute, height));
        layout.RowStyles.Add(new(SizeType.Percent, 70)); layout.RowStyles.Add(new(SizeType.Percent, 30));
        Controls.Add(layout);
        var identity = Bar();
        identity.Controls.AddRange([new Label { Text = "Karakter:", AutoSize = true }, character, new Label { Text = "Sunucu:", AutoSize = true }, server]);
        if (liveMode)
        {
            character.ReadOnly = true; server.ReadOnly = true;
            character.Text = "Kimlik bekleniyor"; server.Text = "—";
            identity.Controls.Add(new Label { Text = "Karakter ve sunucu oyundan algılanır.", AutoSize = true });
        }
        else Button(identity, "Karakteri aç", () => { tracker.Select(new(character.Text.Trim(), server.Text.Trim())); stream = Guid.NewGuid().ToString("N"); frame = 0; });
        layout.Controls.Add(identity, 0, 0);
        var source = Bar();
        if (liveMode)
        {
            startButton = new() { Text = "Canlı okumayı başlat", AutoSize = true };
            stopButton = new() { Text = "Durdur", AutoSize = true, Enabled = false };
            startButton.Click += (_, _) => StartLive();
            stopButton.Click += async (_, _) => await StopLive();
            source.Controls.AddRange([startButton, stopButton, new Label { Text = "Başlattıktan sonra oyunda karaktere giriş yapın.", AutoSize = true }]);
        }
        else
        {
            Button(source, "Demo collection yükle", LoadDemo);
            Button(source, "Veri kaybı simüle et", () => tracker.Gap("Demo paket kaybı"));
            Button(source, "Yeniden bağlan", () => { tracker.Reconnect(); stream = Guid.NewGuid().ToString("N"); frame = 0; });
            source.Controls.Add(new Label { Text = "Bu uygulama oyuna bağlanmaz.", AutoSize = true });
        }
        layout.Controls.Add(source, 0, 1);
        var actions = Bar();
        pets.Items.AddRange(catalog.Pets.Values.OrderBy(p => p.Id).Cast<object>().ToArray());
        pets.SelectedIndexChanged += (_, _) => { if (pets.SelectedItem is PetDefinition p) petId.Value = p.Id; };
        pets.SelectedIndex = 1;
        actions.Controls.AddRange([pets, new Label { Text = "Pet ID:", AutoSize = true }, petId]);
        if (!liveMode)
        {
            actions.Controls.AddRange([new Label { Text = "+Soul:", AutoSize = true }, amount]);
            Button(actions, "Soul üret", () => tracker.Gain(new(tracker.Current.Scope, stream, ++frame, (int)petId.Value, (int)amount.Value)));
        }
        Button(actions, "Pinle", () => tracker.Pin((int)petId.Value, true));
        layout.Controls.Add(actions, 0, 2);
        var rows = Bar();
        Button(rows, "Seçili peti pinle / pin kaldır", () => { var id = SelectedId(); tracker.Pin(id, !tracker.Current.Preferences.Pinned.Contains(id)); });
        Button(rows, "Seçili peti listeden kaldır", () => tracker.Dismiss(SelectedId()));
        rows.Controls.Add(status); layout.Controls.Add(rows, 0, 3);
        foreach (var title in new[] { "Pet ID", "Pet", "Seviye / soul", "Kalan", "Oturum", "Pin" }) grid.Columns.Add(title, title);
        layout.Controls.Add(grid, 0, 4); layout.Controls.Add(debug, 0, 5);
        if (!liveMode) tracker.Select(new(character.Text, server.Text));
        else if (tracker.Saved.Profiles.Values.FirstOrDefault() is { } previous) tracker.Select(previous.Scope);
        RefreshRows();
        poll.Tick += (_, _) => DrainLive();
        if (liveMode) poll.Start();
        FormClosing += async (_, e) =>
        {
            if (allowClose) return;
            e.Cancel = true;
            if (closing) return;
            closing = true;
            try { await StopLive(); store.Save(tracker.Saved); allowClose = true; poll.Stop(); Close(); }
            catch (Exception ex) { closing = false; Log("Kapanış hatası: " + ex.Message); MessageBox.Show(ex.Message, "Kayıt hatası — pencere açık tutuldu"); }
        };
        FormClosed += (_, _) => poll.Dispose();
        Log("207 katalog kaydı (7 Special); oyundaki 200 ile doğrulanmış eşleşme değildir. Demo verileri gerçek ilerleme değildir.");
        if (liveMode) Log(PassiveCapture.NpcapPresent ? "Npcap dosyaları bulundu. Canlı okuma kapalı; Başlat düğmesini kullanın." : "Npcap bulunamadı; kurulum adımları README.md içinde.");
    }
    private static FlowLayoutPanel Bar() => new() { Dock = DockStyle.Fill, Padding = new(6), WrapContents = false, AutoScroll = true };
    private void Button(FlowLayoutPanel panel, string text, Action action)
    {
        var button = new Button { Text = text, AutoSize = true };
        button.Click += (_, _) =>
        {
            try
            {
                if (!tracker.HasCurrent) throw new InvalidOperationException("Önce oyundaki karakter kimliği algılanmalı.");
                action(); store.Save(tracker.Saved);
            }
            catch (Exception ex) { Log($"HATA: {ex.Message}"); MessageBox.Show(ex.Message, "İşlem tamamlanamadı"); }
            RefreshRows();
        };
        panel.Controls.Add(button);
    }
    private int SelectedId() => grid.CurrentRow?.Cells[0].Value is int id ? id : throw new InvalidOperationException("Önce listeden bir pet seçin.");
    private void LoadDemo()
    {
        var entries = catalog.Pets.Keys.Select(id => new CollectionEntry(id, 0, 0)).ToList();
        entries = entries.Select(e => e.PetId switch { 1002 => new CollectionEntry(1002, 0, 4), 1003 => new CollectionEntry(1003, 1, 24), 1004 => new CollectionEntry(1004, 2, 74), 1005 => new CollectionEntry(1005, 2, 46), _ => e }).ToList();
        tracker.Synchronize(new(tracker.Current.Scope, stream, ++frame, entries));
    }
    private void RefreshRows()
    {
        status.Text = tracker.Quality switch { DataQuality.Exact => liveMode ? "Kesin" : "DEMO: Kesin", DataQuality.WaitingForSnapshot => "Collection bekleniyor", _ => "Belirsiz — collection ile doğrulayın" };
        if (liveMode)
        {
            Text = "Aion 2 Pet Tracker — " + liveSession.Status;
            character.Text = tracker.HasCurrent ? tracker.Current.Scope.Character : "Kimlik bekleniyor";
            server.Text = tracker.HasCurrent ? tracker.Current.Scope.Server : "—";
            if (!liveSession.IdentityKnown) status.Text = "Son bilinen / kimlik bekleniyor";
        }
        var selected = grid.CurrentRow?.Cells[0].Value as int?;
        grid.Rows.Clear();
        if (!tracker.HasCurrent) return;
        foreach (var id in tracker.VisibleIds)
        {
            var known = tracker.KnownTotal(id);
            var p = known is { } total ? Progress.FromTotal(total) : null;
            var label = p is null ? "Bilinmiyor" : p.Level == 3 ? "Lv3 MAX" : $"Lv{p.Level} · {p.WithinLevel}/{p.Goal}";
            if (p is not null && tracker.Quality != DataQuality.Exact) label = "Son bilinen: " + label;
            grid.Rows.Add(id, catalog.Name(id), label, p is null ? "—" : (tracker.Quality == DataQuality.Exact ? "" : "Son bilinen: ") + p.Remaining, "+" + tracker.Session.Souls.GetValueOrDefault(id), tracker.Current.Preferences.Pinned.Contains(id) ? "★" : "");
        }
        if (selected is { } chosen)
            foreach (DataGridViewRow row in grid.Rows)
                if (row.Cells[0].Value is int id && id == chosen) { grid.CurrentCell = row.Cells[0]; break; }
    }
    private void StartLive()
    {
        if (capture is not null) return;
        try
        {
            stopping = null;
            capture = new(message =>
            {
                if (messages.Count >= 2048)
                { Interlocked.Exchange(ref overflow, 1); capture?.RequestStop(); return; }
                messages.Enqueue(message);
            });
            capture.Start(); startButton!.Enabled = false; stopButton!.Enabled = true;
        }
        catch (Exception ex) { Log("Başlatma hatası: " + ex.Message); MessageBox.Show(ex.Message); }
    }
    private Task StopLive() => stopping ??= StopLiveCore();
    private async Task StopLiveCore()
    {
        if (capture is null) return;
        var old = capture;
        if (stopButton is not null) stopButton.Enabled = false;
        await old.DisposeAsync(); capture = null;
        do { DrainLive(); } while (!messages.IsEmpty);
        if (startButton is not null) startButton.Enabled = true;
    }
    private void DrainLive()
    {
        var changed = false;
        var lost = Interlocked.Exchange(ref overflow, 0) != 0;
        if (lost)
        {
            while (messages.TryDequeue(out _)) { }
            liveSession.Accept(new StreamEnded("Arayüz kuyruğu doldu; okuma durduruldu. Tam snapshot ile yeniden doğrulayın."));
            changed = true;
        }
        else for (var i = 0; i < 256 && messages.TryDequeue(out var message); i++)
        {
            try { liveSession.Accept(message); }
            catch (Exception ex) { liveSession.Accept(new IntegrityLost("Canlı olay işleme hatası: " + ex.Message)); }
            changed = true;
        }
        if (!changed) return;
        try { store.Save(tracker.Saved); }
        catch (Exception ex) { Log("KAYIT HATASI: " + ex.Message); tracker.Gap("Kalıcı kayıt yazılamadı"); }
        RefreshRows();
    }
    private void Log(string message)
    {
        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz} {message}{Environment.NewLine}";
        debug.AppendText(line);
        if (debug.TextLength > 100_000) debug.Text = debug.Text[^50_000..];
        try { File.AppendAllText(logPath, line); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { debug.AppendText($"LOG DOSYASI HATASI: {ex.Message}{Environment.NewLine}"); }
    }
}
