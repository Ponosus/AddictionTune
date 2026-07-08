import customtkinter as ctk
import os
import sys
import json
import vlc
from yt_dlp import YoutubeDL
import threading
import random

# Фикс для работы VLC после сборки в EXE
if getattr(sys, 'frozen', False):
    base_path = sys._MEIPASS
    os.environ['PYTHON_VLC_LIB_PATH'] = os.path.join(base_path, "libvlc.dll")


class AddictionTune(ctk.CTk):
    def __init__(self):
        super().__init__()

        self.title("AddictionTune")
        self.geometry("900x800")
        self.minsize(800, 700)

        self.config_file = "config.json"
        self.show_onboarding_flag = self.load_config()

        self.current_theme = "dark"
        ctk.set_appearance_mode("dark")

        # Настройка VLC с низким кэшированием для быстрой реакции громкости
        vlc_args = ["--no-xlib", "--quiet", "--network-caching=300", "--no-video"]
        self.vlc_instance = vlc.Instance(" ".join(vlc_args))
        self.player = self.vlc_instance.media_player_new()

        self.current_vol = 120
        self.player.audio_set_volume(self.current_vol)

        self.main_font = "Century Gothic"
        self.media_loaded = False
        self.current_playlist = []
        self.current_idx = 0
        self.is_playing = False
        self.is_dragging = False

        self.main_container = ctk.CTkFrame(self, fg_color="transparent", corner_radius=0)
        self.main_container.place(relx=0, rely=0, relwidth=1, relheight=1)

        self.setup_screens()
        self.setup_mini_player()
        self.setup_top_buttons()
        self.update_ui_loop()

    def load_config(self):
        if not os.path.exists(self.config_file): return True
        try:
            with open(self.config_file, "r") as f:
                return json.load(f).get("show_onboarding", True)
        except:
            return True

    def save_config(self, value):
        with open(self.config_file, "w") as f:
            json.dump({"show_onboarding": value}, f)
        self.show_onboarding_flag = value

    def setup_top_buttons(self):
        self.theme_btn = ctk.CTkButton(self, text="🌙", width=40, height=40, fg_color="transparent", font=("Arial", 20),
                                       command=self.toggle_theme)
        self.faq_btn = ctk.CTkButton(self, text="?", width=40, height=40, fg_color="transparent",
                                     font=(self.main_font, 20, "bold"), command=self.restart_onboarding)
        self.theme_btn.place(relx=0.96, rely=0.04, anchor="center")
        self.faq_btn.place(relx=0.04, rely=0.04, anchor="center")

    def restart_onboarding(self):
        self.go_to_page(1)

    def setup_screens(self):
        self.pages = []
        for i in range(6):
            frame = ctk.CTkFrame(self.main_container, fg_color="transparent", corner_radius=0)
            frame.place(relx=0 if i == 0 else 1, rely=0, relwidth=1, relheight=1)
            self.pages.append(frame)

        # Главный экран (0)
        ctk.CTkLabel(self.pages[0], text="AddictionTune", font=(self.main_font, 70, "bold")).place(relx=0.5, rely=0.35,
                                                                                                   anchor="center")
        ctk.CTkButton(self.pages[0], text="Войти в поток", font=(self.main_font, 22), fg_color="#3b5998", width=320,
                      height=60, command=self.start_flow).place(relx=0.5, rely=0.55, anchor="center")

        # Обучение (1-3)
        self.setup_ob_page(1, "ACTIVE", "#E74C3C", "Высокий темп (Breakcore/Jungle).", 2)
        self.setup_ob_page(2, "FOCUS", "#3498DB",
                           "тяжелые гитары, 8-битные мелодии\n и агрессивная электроника.\nидеально для работы (Maidcore).",
                           3)
        self.setup_ob_page(3, "RELAX", "#2ECC71", "Ambient и Lo-Fi.", 4, is_last=True)

        # Пресеты (4)
        self.back_to_main = ctk.CTkButton(self.pages[4], text="←", font=(self.main_font, 30, "bold"), width=50,
                                          height=50, fg_color="transparent", command=lambda: self.go_to_page(0))
        self.back_to_main.place(relx=0.05, rely=0.05)
        ctk.CTkLabel(self.pages[4], text="атмосфера:", font=(self.main_font, 38, "bold")).place(relx=0.5, rely=0.1,
                                                                                                anchor="center")
        modes = [("ACTIVE", "#E74C3C", "Breakcore Jungle mix"), ("FOCUS", "#3498DB", "Maidcore mix"),
                 ("RELAX", "#2ECC71", "Ambient Lofi")]
        for i, (name, color, query) in enumerate(modes):
            ctk.CTkButton(self.pages[4], text=name, font=(self.main_font, 45, "bold"), fg_color="transparent",
                          text_color=color, hover=False, width=500, height=100,
                          command=lambda q=query: self.start_online(q)).place(relx=0.5, rely=0.35 + i * 0.2,
                                                                              anchor="center")

        self.setup_player_ui(self.pages[5])
        self.current_visible_idx = 0

    def setup_ob_page(self, idx, title, color, desc, next_idx, is_last=False):
        ctk.CTkLabel(self.pages[idx], text=title, font=(self.main_font, 50, "bold"), text_color=color).place(relx=0.5,
                                                                                                             rely=0.25,
                                                                                                             anchor="center")
        ctk.CTkLabel(self.pages[idx], text=desc, font=(self.main_font, 20), justify="center").place(relx=0.5, rely=0.45,
                                                                                                    anchor="center")
        btn_text = "Поехали" if is_last else "Далее"
        cmd = (lambda: (self.save_config(False), self.go_to_page(next_idx))) if is_last else (
            lambda: self.go_to_page(next_idx))
        ctk.CTkButton(self.pages[idx], text=btn_text, width=200, height=50, fg_color="#3b5998", command=cmd).place(
            relx=0.5, rely=0.7, anchor="center")

    def setup_player_ui(self, parent):
        self.back_to_presets = ctk.CTkButton(parent, text="←", font=(self.main_font, 30, "bold"), width=50, height=50,
                                             fg_color="transparent", command=lambda: self.go_to_page(4))
        self.back_to_presets.place(relx=0.05, rely=0.05)

        container = ctk.CTkFrame(parent, fg_color="transparent")
        container.place(relx=0.5, rely=0.5, anchor="center")

        self.online_track = ctk.CTkLabel(container, text="...", font=(self.main_font, 28, "bold"), wraplength=600)
        self.online_track.pack(pady=(10, 5))
        self.online_artist = ctk.CTkLabel(container, text="", font=(self.main_font, 18), text_color="gray")
        self.online_artist.pack(pady=5)

        # Прогресс
        prog_f = ctk.CTkFrame(container, fg_color="transparent")
        prog_f.pack(pady=20)
        self.online_c_time = ctk.CTkLabel(prog_f, text="0:00", font=(self.main_font, 12))
        self.online_c_time.pack(side="left", padx=10)

        self.online_slider = ctk.CTkSlider(prog_f, from_=0, to=100, width=400, height=16)
        self.online_slider.set(0)
        self.online_slider.pack(side="left")
        self.online_slider.bind("<Button-1>", lambda e: setattr(self, 'is_dragging', True))
        self.online_slider.bind("<ButtonRelease-1>", self.seek_music_final)

        self.online_d_time = ctk.CTkLabel(prog_f, text="0:00", font=(self.main_font, 12))
        self.online_d_time.pack(side="left", padx=10)

        # Управление (отступы увеличены, ничего не наезжает)
        ctrl = ctk.CTkFrame(container, fg_color="transparent")
        ctrl.pack(pady=15)
        ctk.CTkButton(ctrl, text="⏮", width=60, font=("Arial", 35), fg_color="transparent", command=self.prev_tr).pack(
            side="left", padx=45)

        self.online_play_btn = ctk.CTkButton(ctrl, text="▶", width=90, height=90, corner_radius=45, fg_color="#3b5998",
                                             font=("Arial", 35), command=self.toggle_music)
        self.online_play_btn.pack(side="left", padx=45)

        ctk.CTkButton(ctrl, text="⏭", width=60, font=("Arial", 35), fg_color="transparent", command=self.next_tr).pack(
            side="left", padx=45)

        # Громкость (выровнена идеально)
        vol_container = ctk.CTkFrame(container, fg_color="transparent")
        vol_container.pack(pady=20)

        ctk.CTkLabel(vol_container, text="🔈", font=("Arial", 16)).pack(side="left", padx=(0, 10))
        self.vol_slider = ctk.CTkSlider(vol_container, from_=0, to=240, width=250, height=16, command=self.set_volume)
        self.vol_slider.set(self.current_vol)
        self.vol_slider.pack(side="left")
        ctk.CTkLabel(vol_container, text="🔊", font=("Arial", 16)).pack(side="left", padx=(10, 0))

    def setup_mini_player(self):
        self.mini_bar = ctk.CTkFrame(self, width=680, height=75, corner_radius=20, fg_color=("#D1D1D1", "#141414"),
                                     border_width=1, border_color="#3b5998")

        self.mini_track = ctk.CTkButton(self.mini_bar, text="...", font=(self.main_font, 11, "bold"),
                                        fg_color="transparent", anchor="w", command=lambda: self.go_to_page(5))
        self.mini_track.place(relx=0.03, rely=0.5, relwidth=0.32, anchor="w")

        ctk.CTkButton(self.mini_bar, text="⏮", width=30, fg_color="transparent", font=("Arial", 18),
                      command=self.prev_tr).place(relx=0.40, rely=0.5, anchor="center")
        self.mini_play_btn = ctk.CTkButton(self.mini_bar, text="▶", width=40, height=40, corner_radius=20,
                                           fg_color="#3b5998", font=("Arial", 16), command=self.toggle_music)
        self.mini_play_btn.place(relx=0.48, rely=0.5, anchor="center")
        ctk.CTkButton(self.mini_bar, text="⏭", width=30, fg_color="transparent", font=("Arial", 18),
                      command=self.next_tr).place(relx=0.56, rely=0.5, anchor="center")

        ctk.CTkLabel(self.mini_bar, text="🔈", font=("Arial", 12)).place(relx=0.64, rely=0.5, anchor="center")
        self.mini_vol_slider = ctk.CTkSlider(self.mini_bar, from_=0, to=240, width=120, height=12,
                                             command=self.set_volume)
        self.mini_vol_slider.set(self.current_vol)
        self.mini_vol_slider.place(relx=0.77, rely=0.5, anchor="center")

        ctk.CTkButton(self.mini_bar, text="✕", width=25, fg_color="transparent", text_color="gray",
                      command=self.reset_ui).place(relx=0.94, rely=0.5, anchor="center")

    def set_volume(self, v):
        new_vol = int(float(v))
        self.player.audio_set_volume(new_vol)
        self.current_vol = new_vol
        if hasattr(self, 'vol_slider'): self.vol_slider.set(new_vol)
        if hasattr(self, 'mini_vol_slider'): self.mini_vol_slider.set(new_vol)

    def seek_music_final(self, event):
        if self.media_loaded:
            self.player.set_position(float(self.online_slider.get()) / 100)
        self.is_dragging = False

    def start_online(self, q):
        self.player.stop()
        self.media_loaded = False
        self.go_to_page(5)
        self.online_track.configure(text="Поиск...")
        threading.Thread(target=self._search, args=(q,), daemon=True).start()

    def _search(self, q):
        try:
            with YoutubeDL({'format': 'bestaudio', 'quiet': True, 'no_warnings': True}) as ydl:
                res = ydl.extract_info(f"ytsearch10:{q}", download=False)
                if 'entries' in res:
                    self.current_playlist = [{'id': e['id'], 'title': e['title'], 'artist': e.get('uploader', 'Music')}
                                             for e in res['entries']]
                    random.shuffle(self.current_playlist)
                    self.current_idx = 0
                    self.after(0, self.play_idx)
        except:
            pass

    def play_idx(self):
        if not self.current_playlist: return
        t = self.current_playlist[self.current_idx]

        def load():
            try:
                with YoutubeDL({'format': 'bestaudio/best', 'quiet': True}) as ydl:
                    info = ydl.extract_info(t['id'], download=False)
                    url = info['url']
                self.after(0, lambda: self._start_vlc(url, t))
            except:
                self.after(0, self.next_tr)

        threading.Thread(target=load, daemon=True).start()

    def _start_vlc(self, url, t):
        self.player.set_media(self.vlc_instance.media_new(url))
        self.player.play()
        self.media_loaded = True
        self.is_playing = True
        self.online_track.configure(text=t['title'])
        self.online_artist.configure(text=t['artist'])
        self.mini_track.configure(text=t['title'][:25] + "...")
        self._sync_play_state()

    def toggle_music(self):
        if not self.media_loaded: return
        if self.player.get_state() == vlc.State.Playing:
            self.player.pause()
            self.is_playing = False
        else:
            self.player.play()
            self.is_playing = True
        self._sync_play_state()

    def _sync_play_state(self):
        char = "⏸" if self.is_playing else "▶"
        self.online_play_btn.configure(text=char)
        self.mini_play_btn.configure(text=char)

    def update_ui_loop(self):
        try:
            if self.player.get_state() == vlc.State.Ended:
                self.next_tr()
            elif self.media_loaded and not self.is_dragging:
                ms, total = self.player.get_time(), self.player.get_length()
                if total > 0:
                    self.online_slider.set((ms / total) * 100)
                    cur, dur = ms // 1000, total // 1000
                    self.online_c_time.configure(text=f"{cur // 60}:{str(cur % 60).zfill(2)}")
                    self.online_d_time.configure(text=f"{dur // 60}:{str(dur % 60).zfill(2)}")
        except:
            pass
        self.after(500, self.update_ui_loop)

    def next_tr(self):
        if self.current_playlist:
            self.current_idx = (self.current_idx + 1) % len(self.current_playlist)
            self.play_idx()

    def prev_tr(self):
        if self.current_playlist:
            self.current_idx = (self.current_idx - 1) % len(self.current_playlist)
            self.play_idx()

    def go_to_page(self, target_idx):
        if target_idx == 0:
            self.theme_btn.place(relx=0.96, rely=0.04, anchor="center")
            self.faq_btn.place(relx=0.04, rely=0.04, anchor="center")
        else:
            self.theme_btn.place_forget();
            self.faq_btn.place_forget()

        if target_idx != 5 and self.media_loaded:
            self.mini_bar.place(relx=0.5, rely=0.93, anchor="center")
        else:
            self.mini_bar.place_forget()

        old_page, new_page = self.pages[self.current_visible_idx], self.pages[target_idx]
        direction = 1 if target_idx > self.current_visible_idx else -1
        new_page.place(relx=direction)
        for i in range(1, 11):
            offset = (i / 10) * direction
            self.after(i * 15, lambda o=offset: (old_page.place(relx=-o), new_page.place(relx=direction - o)))
        self.current_visible_idx = target_idx

    def start_flow(self):
        self.go_to_page(1 if self.show_onboarding_flag else 4)

    def toggle_theme(self):
        self.current_theme = "light" if self.current_theme == "dark" else "dark"
        ctk.set_appearance_mode(self.current_theme)
        color = "black" if self.current_theme == "light" else "white"
        self.theme_btn.configure(text="☀" if self.current_theme == "light" else "🌙", text_color=color)
        self.faq_btn.configure(text_color=color)
        self.back_to_main.configure(text_color=color)
        self.back_to_presets.configure(text_color=color)

    def reset_ui(self):
        self.player.stop();
        self.media_loaded = False;
        self.mini_bar.place_forget()


if __name__ == "__main__":
    app = AddictionTune()
    app.mainloop()
