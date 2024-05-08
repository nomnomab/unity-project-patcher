FFmpeg 64-bit static Windows build from www.gyan.dev

Version: 2024-04-29-git-cf4af4bca0-essentials_build-www.gyan.dev

License: GPL v3

Source Code: https://github.com/FFmpeg/FFmpeg/commit/cf4af4bca0

git-essentials build configuration: 

ARCH                      x86 (generic)
big-endian                no
runtime cpu detection     yes
standalone assembly       yes
x86 assembler             nasm
MMX enabled               yes
MMXEXT enabled            yes
3DNow! enabled            yes
3DNow! extended enabled   yes
SSE enabled               yes
SSSE3 enabled             yes
AESNI enabled             yes
AVX enabled               yes
AVX2 enabled              yes
AVX-512 enabled           yes
AVX-512ICL enabled        yes
XOP enabled               yes
FMA3 enabled              yes
FMA4 enabled              yes
i686 features enabled     yes
CMOV is fast              yes
EBX available             yes
EBP available             yes
debug symbols             yes
strip symbols             yes
optimize for size         no
optimizations             yes
static                    yes
shared                    no
postprocessing support    yes
network support           yes
threading support         pthreads
safe bitstream reader     yes
texi2html enabled         no
perl enabled              yes
pod2man enabled           yes
makeinfo enabled          yes
makeinfo supports HTML    yes
xmllint enabled           yes

External libraries:
avisynth                libopencore_amrnb       libvpx
bzlib                   libopencore_amrwb       libwebp
gmp                     libopenjpeg             libx264
gnutls                  libopenmpt              libx265
iconv                   libopus                 libxml2
libaom                  librubberband           libxvid
libass                  libspeex                libzimg
libfontconfig           libsrt                  libzmq
libfreetype             libssh                  lzma
libfribidi              libtheora               mediafoundation
libgme                  libvidstab              sdl2
libgsm                  libvmaf                 zlib
libharfbuzz             libvo_amrwbenc
libmp3lame              libvorbis

External libraries providing hardware acceleration:
amf                     d3d12va                 nvdec
cuda                    dxva2                   nvenc
cuda_llvm               ffnvcodec               vaapi
cuvid                   libmfx
d3d11va                 libvpl

Libraries:
avcodec                 avformat                swresample
avdevice                avutil                  swscale
avfilter                postproc

Programs:
ffmpeg                  ffplay                  ffprobe

Enabled decoders:
aac                     fraps                   pgm
aac_fixed               frwu                    pgmyuv
aac_latm                ftr                     pgssub
aasc                    g2m                     pgx
ac3                     g723_1                  phm
ac3_fixed               g729                    photocd
acelp_kelvin            gdv                     pictor
adpcm_4xm               gem                     pixlet
adpcm_adx               gif                     pjs
adpcm_afc               gremlin_dpcm            png
adpcm_agm               gsm                     ppm
adpcm_aica              gsm_ms                  prores
adpcm_argo              h261                    prosumer
adpcm_ct                h263                    psd
adpcm_dtk               h263i                   ptx
adpcm_ea                h263p                   qcelp
adpcm_ea_maxis_xa       h264                    qdm2
adpcm_ea_r1             h264_cuvid              qdmc
adpcm_ea_r2             h264_qsv                qdraw
adpcm_ea_r3             hap                     qoa
adpcm_ea_xas            hca                     qoi
adpcm_g722              hcom                    qpeg
adpcm_g726              hdr                     qtrle
adpcm_g726le            hevc                    r10k
adpcm_ima_acorn         hevc_cuvid              r210
adpcm_ima_alp           hevc_qsv                ra_144
adpcm_ima_amv           hnm4_video              ra_288
adpcm_ima_apc           hq_hqa                  ralf
adpcm_ima_apm           hqx                     rasc
adpcm_ima_cunning       huffyuv                 rawvideo
adpcm_ima_dat4          hymt                    realtext
adpcm_ima_dk3           iac                     rka
adpcm_ima_dk4           idcin                   rl2
adpcm_ima_ea_eacs       idf                     roq
adpcm_ima_ea_sead       iff_ilbm                roq_dpcm
adpcm_ima_iss           ilbc                    rpza
adpcm_ima_moflex        imc                     rscc
adpcm_ima_mtf           imm4                    rtv1
adpcm_ima_oki           imm5                    rv10
adpcm_ima_qt            indeo2                  rv20
adpcm_ima_rad           indeo3                  rv30
adpcm_ima_smjpeg        indeo4                  rv40
adpcm_ima_ssi           indeo5                  s302m
adpcm_ima_wav           interplay_acm           sami
adpcm_ima_ws            interplay_dpcm          sanm
adpcm_ms                interplay_video         sbc
adpcm_mtaf              ipu                     scpr
adpcm_psx               jacosub                 screenpresso
adpcm_sbpro_2           jpeg2000                sdx2_dpcm
adpcm_sbpro_3           jpegls                  sga
adpcm_sbpro_4           jv                      sgi
adpcm_swf               kgv1                    sgirle
adpcm_thp               kmvc                    sheervideo
adpcm_thp_le            lagarith                shorten
adpcm_vima              lead                    simbiosis_imx
adpcm_xa                libaom_av1              sipr
adpcm_xmd               libgsm                  siren
adpcm_yamaha            libgsm_ms               smackaud
adpcm_zork              libopencore_amrnb       smacker
agm                     libopencore_amrwb       smc
aic                     libopus                 smvjpeg
alac                    libspeex                snow
alias_pix               libvorbis               sol_dpcm
als                     libvpx_vp8              sonic
amrnb                   libvpx_vp9              sp5x
amrwb                   loco                    speedhq
amv                     lscr                    speex
anm                     m101                    srgc
ansi                    mace3                   srt
anull                   mace6                   ssa
apac                    magicyuv                stl
ape                     mdec                    subrip
apng                    media100                subviewer
aptx                    metasound               subviewer1
aptx_hd                 microdvd                sunrast
arbc                    mimic                   svq1
argo                    misc4                   svq3
ass                     mjpeg                   tak
asv1                    mjpeg_cuvid             targa
asv2                    mjpeg_qsv               targa_y216
atrac1                  mjpegb                  tdsc
atrac3                  mlp                     text
atrac3al                mmvideo                 theora
atrac3p                 mobiclip                thp
atrac3pal               motionpixels            tiertexseqvideo
atrac9                  movtext                 tiff
aura                    mp1                     tmv
aura2                   mp1float                truehd
av1                     mp2                     truemotion1
av1_cuvid               mp2float                truemotion2
av1_qsv                 mp3                     truemotion2rt
avrn                    mp3adu                  truespeech
avrp                    mp3adufloat             tscc
avs                     mp3float                tscc2
avui                    mp3on4                  tta
bethsoftvid             mp3on4float             twinvq
bfi                     mpc7                    txd
bink                    mpc8                    ulti
binkaudio_dct           mpeg1_cuvid             utvideo
binkaudio_rdft          mpeg1video              v210
bintext                 mpeg2_cuvid             v210x
bitpacked               mpeg2_qsv               v308
bmp                     mpeg2video              v408
bmv_audio               mpeg4                   v410
bmv_video               mpeg4_cuvid             vb
bonk                    mpegvideo               vble
brender_pix             mpl2                    vbn
c93                     msa1                    vc1
cavs                    mscc                    vc1_cuvid
cbd2_dpcm               msmpeg4v1               vc1_qsv
ccaption                msmpeg4v2               vc1image
cdgraphics              msmpeg4v3               vcr1
cdtoons                 msnsiren                vmdaudio
cdxl                    msp2                    vmdvideo
cfhd                    msrle                   vmix
cinepak                 mss1                    vmnc
clearvideo              mss2                    vnull
cljr                    msvideo1                vorbis
cllc                    mszh                    vp3
comfortnoise            mts2                    vp4
cook                    mv30                    vp5
cpia                    mvc1                    vp6
cri                     mvc2                    vp6a
cscd                    mvdv                    vp6f
cyuv                    mvha                    vp7
dca                     mwsc                    vp8
dds                     mxpeg                   vp8_cuvid
derf_dpcm               nellymoser              vp8_qsv
dfa                     notchlc                 vp9
dfpwm                   nuv                     vp9_cuvid
dirac                   on2avc                  vp9_qsv
dnxhd                   opus                    vplayer
dolby_e                 osq                     vqa
dpx                     paf_audio               vqc
dsd_lsbf                paf_video               vvc
dsd_lsbf_planar         pam                     wady_dpcm
dsd_msbf                pbm                     wavarc
dsd_msbf_planar         pcm_alaw                wavpack
dsicinaudio             pcm_bluray              wbmp
dsicinvideo             pcm_dvd                 wcmv
dss_sp                  pcm_f16le               webp
dst                     pcm_f24le               webvtt
dvaudio                 pcm_f32be               wmalossless
dvbsub                  pcm_f32le               wmapro
dvdsub                  pcm_f64be               wmav1
dvvideo                 pcm_f64le               wmav2
dxa                     pcm_lxf                 wmavoice
dxtory                  pcm_mulaw               wmv1
dxv                     pcm_s16be               wmv2
eac3                    pcm_s16be_planar        wmv3
eacmv                   pcm_s16le               wmv3image
eamad                   pcm_s16le_planar        wnv1
eatgq                   pcm_s24be               wrapped_avframe
eatgv                   pcm_s24daud             ws_snd1
eatqi                   pcm_s24le               xan_dpcm
eightbps                pcm_s24le_planar        xan_wc3
eightsvx_exp            pcm_s32be               xan_wc4
eightsvx_fib            pcm_s32le               xbin
escape124               pcm_s32le_planar        xbm
escape130               pcm_s64be               xface
evrc                    pcm_s64le               xl
exr                     pcm_s8                  xma1
fastaudio               pcm_s8_planar           xma2
ffv1                    pcm_sga                 xpm
ffvhuff                 pcm_u16be               xsub
ffwavesynth             pcm_u16le               xwd
fic                     pcm_u24be               y41p
fits                    pcm_u24le               ylc
flac                    pcm_u32be               yop
flashsv                 pcm_u32le               yuv4
flashsv2                pcm_u8                  zero12v
flic                    pcm_vidc                zerocodec
flv                     pcx                     zlib
fmvc                    pdv                     zmbv
fourxm                  pfm

Enabled encoders:
a64multi                hevc_nvenc              pcm_u24le
a64multi5               hevc_qsv                pcm_u32be
aac                     hevc_vaapi              pcm_u32le
aac_mf                  huffyuv                 pcm_u8
ac3                     jpeg2000                pcm_vidc
ac3_fixed               jpegls                  pcx
ac3_mf                  libaom_av1              pfm
adpcm_adx               libgsm                  pgm
adpcm_argo              libgsm_ms               pgmyuv
adpcm_g722              libmp3lame              phm
adpcm_g726              libopencore_amrnb       png
adpcm_g726le            libopenjpeg             ppm
adpcm_ima_alp           libopus                 prores
adpcm_ima_amv           libspeex                prores_aw
adpcm_ima_apm           libtheora               prores_ks
adpcm_ima_qt            libvo_amrwbenc          qoi
adpcm_ima_ssi           libvorbis               qtrle
adpcm_ima_wav           libvpx_vp8              r10k
adpcm_ima_ws            libvpx_vp9              r210
adpcm_ms                libwebp                 ra_144
adpcm_swf               libwebp_anim            rawvideo
adpcm_yamaha            libx264                 roq
alac                    libx264rgb              roq_dpcm
alias_pix               libx265                 rpza
amv                     libxvid                 rv10
anull                   ljpeg                   rv20
apng                    magicyuv                s302m
aptx                    mjpeg                   sbc
aptx_hd                 mjpeg_qsv               sgi
ass                     mjpeg_vaapi             smc
asv1                    mlp                     snow
asv2                    movtext                 sonic
av1_amf                 mp2                     sonic_ls
av1_nvenc               mp2fixed                speedhq
av1_qsv                 mp3_mf                  srt
av1_vaapi               mpeg1video              ssa
avrp                    mpeg2_qsv               subrip
avui                    mpeg2_vaapi             sunrast
bitpacked               mpeg2video              svq1
bmp                     mpeg4                   targa
cfhd                    msmpeg4v2               text
cinepak                 msmpeg4v3               tiff
cljr                    msrle                   truehd
comfortnoise            msvideo1                tta
dca                     nellymoser              ttml
dfpwm                   opus                    utvideo
dnxhd                   pam                     v210
dpx                     pbm                     v308
dvbsub                  pcm_alaw                v408
dvdsub                  pcm_bluray              v410
dvvideo                 pcm_dvd                 vbn
dxv                     pcm_f32be               vc2
eac3                    pcm_f32le               vnull
exr                     pcm_f64be               vorbis
ffv1                    pcm_f64le               vp8_vaapi
ffvhuff                 pcm_mulaw               vp9_qsv
fits                    pcm_s16be               vp9_vaapi
flac                    pcm_s16be_planar        wavpack
flashsv                 pcm_s16le               wbmp
flashsv2                pcm_s16le_planar        webvtt
flv                     pcm_s24be               wmav1
g723_1                  pcm_s24daud             wmav2
gif                     pcm_s24le               wmv1
h261                    pcm_s24le_planar        wmv2
h263                    pcm_s32be               wrapped_avframe
h263p                   pcm_s32le               xbm
h264_amf                pcm_s32le_planar        xface
h264_mf                 pcm_s64be               xsub
h264_nvenc              pcm_s64le               xwd
h264_qsv                pcm_s8                  y41p
h264_vaapi              pcm_s8_planar           yuv4
hdr                     pcm_u16be               zlib
hevc_amf                pcm_u16le               zmbv
hevc_mf                 pcm_u24be

Enabled hwaccels:
av1_d3d11va             hevc_nvdec              vc1_nvdec
av1_d3d11va2            hevc_vaapi              vc1_vaapi
av1_d3d12va             mjpeg_nvdec             vp8_nvdec
av1_dxva2               mjpeg_vaapi             vp8_vaapi
av1_nvdec               mpeg1_nvdec             vp9_d3d11va
av1_vaapi               mpeg2_d3d11va           vp9_d3d11va2
h263_vaapi              mpeg2_d3d11va2          vp9_d3d12va
h264_d3d11va            mpeg2_d3d12va           vp9_dxva2
h264_d3d11va2           mpeg2_dxva2             vp9_nvdec
h264_d3d12va            mpeg2_nvdec             vp9_vaapi
h264_dxva2              mpeg2_vaapi             wmv3_d3d11va
h264_nvdec              mpeg4_nvdec             wmv3_d3d11va2
h264_vaapi              mpeg4_vaapi             wmv3_d3d12va
hevc_d3d11va            vc1_d3d11va             wmv3_dxva2
hevc_d3d11va2           vc1_d3d11va2            wmv3_nvdec
hevc_d3d12va            vc1_d3d12va             wmv3_vaapi
hevc_dxva2              vc1_dxva2

Enabled parsers:
aac                     dvdsub                  mpegaudio
aac_latm                evc                     mpegvideo
ac3                     flac                    opus
adx                     ftr                     png
amr                     g723_1                  pnm
av1                     g729                    qoi
avs2                    gif                     rv34
avs3                    gsm                     sbc
bmp                     h261                    sipr
cavsvideo               h263                    tak
cook                    h264                    vc1
cri                     hdr                     vorbis
dca                     hevc                    vp3
dirac                   ipu                     vp8
dnxhd                   jpeg2000                vp9
dolby_e                 jpegxl                  vvc
dpx                     misc4                   webp
dvaudio                 mjpeg                   xbm
dvbsub                  mlp                     xma
dvd_nav                 mpeg4video              xwd

Enabled demuxers:
aa                      idf                     pcm_mulaw
aac                     iff                     pcm_s16be
aax                     ifv                     pcm_s16le
ac3                     ilbc                    pcm_s24be
ac4                     image2                  pcm_s24le
ace                     image2_alias_pix        pcm_s32be
acm                     image2_brender_pix      pcm_s32le
act                     image2pipe              pcm_s8
adf                     image_bmp_pipe          pcm_u16be
adp                     image_cri_pipe          pcm_u16le
ads                     image_dds_pipe          pcm_u24be
adx                     image_dpx_pipe          pcm_u24le
aea                     image_exr_pipe          pcm_u32be
afc                     image_gem_pipe          pcm_u32le
aiff                    image_gif_pipe          pcm_u8
aix                     image_hdr_pipe          pcm_vidc
alp                     image_j2k_pipe          pdv
amr                     image_jpeg_pipe         pjs
amrnb                   image_jpegls_pipe       pmp
amrwb                   image_jpegxl_pipe       pp_bnk
anm                     image_pam_pipe          pva
apac                    image_pbm_pipe          pvf
apc                     image_pcx_pipe          qcp
ape                     image_pfm_pipe          qoa
apm                     image_pgm_pipe          r3d
apng                    image_pgmyuv_pipe       rawvideo
aptx                    image_pgx_pipe          rcwt
aptx_hd                 image_phm_pipe          realtext
aqtitle                 image_photocd_pipe      redspark
argo_asf                image_pictor_pipe       rka
argo_brp                image_png_pipe          rl2
argo_cvg                image_ppm_pipe          rm
asf                     image_psd_pipe          roq
asf_o                   image_qdraw_pipe        rpl
ass                     image_qoi_pipe          rsd
ast                     image_sgi_pipe          rso
au                      image_sunrast_pipe      rtp
av1                     image_svg_pipe          rtsp
avi                     image_tiff_pipe         s337m
avisynth                image_vbn_pipe          sami
avr                     image_webp_pipe         sap
avs                     image_xbm_pipe          sbc
avs2                    image_xpm_pipe          sbg
avs3                    image_xwd_pipe          scc
bethsoftvid             imf                     scd
bfi                     ingenient               sdns
bfstm                   ipmovie                 sdp
bink                    ipu                     sdr2
binka                   ircam                   sds
bintext                 iss                     sdx
bit                     iv8                     segafilm
bitpacked               ivf                     ser
bmv                     ivr                     sga
boa                     jacosub                 shorten
bonk                    jpegxl_anim             siff
brstm                   jv                      simbiosis_imx
c93                     kux                     sln
caf                     kvag                    smacker
cavsvideo               laf                     smjpeg
cdg                     lc3                     smush
cdxl                    libgme                  sol
cine                    libopenmpt              sox
codec2                  live_flv                spdif
codec2raw               lmlm4                   srt
concat                  loas                    stl
dash                    lrc                     str
data                    luodat                  subviewer
daud                    lvf                     subviewer1
dcstr                   lxf                     sup
derf                    m4v                     svag
dfa                     matroska                svs
dfpwm                   mca                     swf
dhav                    mcc                     tak
dirac                   mgsts                   tedcaptions
dnxhd                   microdvd                thp
dsf                     mjpeg                   threedostr
dsicin                  mjpeg_2000              tiertexseq
dss                     mlp                     tmv
dts                     mlv                     truehd
dtshd                   mm                      tta
dv                      mmf                     tty
dvbsub                  mods                    txd
dvbtxt                  moflex                  ty
dxa                     mov                     usm
ea                      mp3                     v210
ea_cdata                mpc                     v210x
eac3                    mpc8                    vag
epaf                    mpegps                  vc1
evc                     mpegts                  vc1t
ffmetadata              mpegtsraw               vividas
filmstrip               mpegvideo               vivo
fits                    mpjpeg                  vmd
flac                    mpl2                    vobsub
flic                    mpsub                   voc
flv                     msf                     vpk
fourxm                  msnwc_tcp               vplayer
frm                     msp                     vqf
fsb                     mtaf                    vvc
fwse                    mtv                     w64
g722                    musx                    wady
g723_1                  mv                      wav
g726                    mvi                     wavarc
g726le                  mxf                     wc3
g729                    mxg                     webm_dash_manifest
gdv                     nc                      webvtt
genh                    nistsphere              wsaud
gif                     nsp                     wsd
gsm                     nsv                     wsvqa
gxf                     nut                     wtv
h261                    nuv                     wv
h263                    obu                     wve
h264                    ogg                     xa
hca                     oma                     xbin
hcom                    osq                     xmd
hevc                    paf                     xmv
hls                     pcm_alaw                xvag
hnm                     pcm_f32be               xwma
iamf                    pcm_f32le               yop
ico                     pcm_f64be               yuv4mpegpipe
idcin                   pcm_f64le

Enabled muxers:
a64                     h263                    pcm_s16le
ac3                     h264                    pcm_s24be
ac4                     hash                    pcm_s24le
adts                    hds                     pcm_s32be
adx                     hevc                    pcm_s32le
aea                     hls                     pcm_s8
aiff                    iamf                    pcm_u16be
alp                     ico                     pcm_u16le
amr                     ilbc                    pcm_u24be
amv                     image2                  pcm_u24le
apm                     image2pipe              pcm_u32be
apng                    ipod                    pcm_u32le
aptx                    ircam                   pcm_u8
aptx_hd                 ismv                    pcm_vidc
argo_asf                ivf                     psp
argo_cvg                jacosub                 rawvideo
asf                     kvag                    rcwt
asf_stream              latm                    rm
ass                     lc3                     roq
ast                     lrc                     rso
au                      m4v                     rtp
avi                     matroska                rtp_mpegts
avif                    matroska_audio          rtsp
avm2                    md5                     sap
avs2                    microdvd                sbc
avs3                    mjpeg                   scc
bit                     mkvtimestamp_v2         segafilm
caf                     mlp                     segment
cavsvideo               mmf                     smjpeg
codec2                  mov                     smoothstreaming
codec2raw               mp2                     sox
crc                     mp3                     spdif
dash                    mp4                     spx
data                    mpeg1system             srt
daud                    mpeg1vcd                stream_segment
dfpwm                   mpeg1video              streamhash
dirac                   mpeg2dvd                sup
dnxhd                   mpeg2svcd               swf
dts                     mpeg2video              tee
dv                      mpeg2vob                tg2
eac3                    mpegts                  tgp
evc                     mpjpeg                  truehd
f4v                     mxf                     tta
ffmetadata              mxf_d10                 ttml
fifo                    mxf_opatom              uncodedframecrc
filmstrip               null                    vc1
fits                    nut                     vc1t
flac                    obu                     voc
flv                     oga                     vvc
framecrc                ogg                     w64
framehash               ogv                     wav
framemd5                oma                     webm
g722                    opus                    webm_chunk
g723_1                  pcm_alaw                webm_dash_manifest
g726                    pcm_f32be               webp
g726le                  pcm_f32le               webvtt
gif                     pcm_f64be               wsaud
gsm                     pcm_f64le               wtv
gxf                     pcm_mulaw               wv
h261                    pcm_s16be               yuv4mpegpipe

Enabled protocols:
async                   http                    rtmp
cache                   httpproxy               rtmpe
concat                  https                   rtmps
concatf                 icecast                 rtmpt
crypto                  ipfs_gateway            rtmpte
data                    ipns_gateway            rtmpts
fd                      libsrt                  rtp
ffrtmpcrypt             libssh                  srtp
ffrtmphttp              libzmq                  subfile
file                    md5                     tcp
ftp                     mmsh                    tee
gopher                  mmst                    tls
gophers                 pipe                    udp
hls                     prompeg                 udplite

Enabled filters:
a3dscope                datascope               pal100bars
aap                     dblur                   pal75bars
abench                  dcshift                 palettegen
abitscope               dctdnoiz                paletteuse
acompressor             ddagrab                 pan
acontrast               deband                  perms
acopy                   deblock                 perspective
acrossfade              decimate                phase
acrossover              deconvolve              photosensitivity
acrusher                dedot                   pixdesctest
acue                    deesser                 pixelize
addroi                  deflate                 pixscope
adeclick                deflicker               pp
adeclip                 deinterlace_qsv         pp7
adecorrelate            deinterlace_vaapi       premultiply
adelay                  dejudder                prewitt
adenorm                 delogo                  procamp_vaapi
aderivative             denoise_vaapi           pseudocolor
adrawgraph              derain                  psnr
adrc                    deshake                 pullup
adynamicequalizer       despill                 qp
adynamicsmooth          detelecine              random
aecho                   dialoguenhance          readeia608
aemphasis               dilation                readvitc
aeval                   displace                realtime
aevalsrc                dnn_classify            remap
aexciter                dnn_detect              removegrain
afade                   dnn_processing          removelogo
afdelaysrc              doubleweave             repeatfields
afftdn                  drawbox                 replaygain
afftfilt                drawbox_vaapi           reverse
afir                    drawgraph               rgbashift
afireqsrc               drawgrid                rgbtestsrc
afirsrc                 drawtext                roberts
aformat                 drmeter                 rotate
afreqshift              dynaudnorm              rubberband
afwtdn                  earwax                  sab
agate                   ebur128                 scale
agraphmonitor           edgedetect              scale2ref
ahistogram              elbg                    scale_cuda
aiir                    entropy                 scale_qsv
aintegral               epx                     scale_vaapi
ainterleave             eq                      scdet
alatency                equalizer               scharr
alimiter                erosion                 scroll
allpass                 estdif                  segment
allrgb                  exposure                select
allyuv                  extractplanes           selectivecolor
aloop                   extrastereo             sendcmd
alphaextract            fade                    separatefields
alphamerge              feedback                setdar
amerge                  fftdnoiz                setfield
ametadata               fftfilt                 setparams
amix                    field                   setpts
amovie                  fieldhint               setrange
amplify                 fieldmatch              setsar
amultiply               fieldorder              settb
anequalizer             fillborders             sharpness_vaapi
anlmdn                  find_rect               shear
anlmf                   firequalizer            showcqt
anlms                   flanger                 showcwt
anoisesrc               floodfill               showfreqs
anull                   format                  showinfo
anullsink               fps                     showpalette
anullsrc                framepack               showspatial
apad                    framerate               showspectrum
aperms                  framestep               showspectrumpic
aphasemeter             freezedetect            showvolume
aphaser                 freezeframes            showwaves
aphaseshift             fspp                    showwavespic
apsnr                   fsync                   shuffleframes
apsyclip                gblur                   shufflepixels
apulsator               geq                     shuffleplanes
arealtime               gradfun                 sidechaincompress
aresample               gradients               sidechaingate
areverse                graphmonitor            sidedata
arls                    grayworld               sierpinski
arnndn                  greyedge                signalstats
asdr                    guided                  signature
asegment                haas                    silencedetect
aselect                 haldclut                silenceremove
asendcmd                haldclutsrc             sinc
asetnsamples            hdcd                    sine
asetpts                 headphone               siti
asetrate                hflip                   smartblur
asettb                  highpass                smptebars
ashowinfo               highshelf               smptehdbars
asidedata               hilbert                 sobel
asisdr                  histeq                  spectrumsynth
asoftclip               histogram               speechnorm
aspectralstats          hqdn3d                  split
asplit                  hqx                     spp
ass                     hstack                  sr
astats                  hstack_qsv              ssim
astreamselect           hstack_vaapi            ssim360
asubboost               hsvhold                 stereo3d
asubcut                 hsvkey                  stereotools
asupercut               hue                     stereowiden
asuperpass              huesaturation           streamselect
asuperstop              hwdownload              subtitles
atadenoise              hwmap                   super2xsai
atempo                  hwupload                superequalizer
atilt                   hwupload_cuda           surround
atrim                   hysteresis              swaprect
avectorscope            identity                swapuv
avgblur                 idet                    tblend
avsynctest              il                      telecine
axcorrelate             inflate                 testsrc
azmq                    interlace               testsrc2
backgroundkey           interleave              thistogram
bandpass                join                    threshold
bandreject              kerndeint               thumbnail
bass                    kirsch                  thumbnail_cuda
bbox                    lagfun                  tile
bench                   latency                 tiltandshift
bilateral               lenscorrection          tiltshelf
bilateral_cuda          libvmaf                 tinterlace
biquad                  life                    tlut2
bitplanenoise           limitdiff               tmedian
blackdetect             limiter                 tmidequalizer
blackframe              loop                    tmix
blend                   loudnorm                tonemap
blockdetect             lowpass                 tonemap_vaapi
blurdetect              lowshelf                tpad
bm3d                    lumakey                 transpose
boxblur                 lut                     transpose_vaapi
bwdif                   lut1d                   treble
bwdif_cuda              lut2                    tremolo
cas                     lut3d                   trim
ccrepack                lutrgb                  unpremultiply
cellauto                lutyuv                  unsharp
channelmap              mandelbrot              untile
channelsplit            maskedclamp             uspp
chorus                  maskedmax               v360
chromahold              maskedmerge             vaguedenoiser
chromakey               maskedmin               varblur
chromakey_cuda          maskedthreshold         vectorscope
chromanr                maskfun                 vflip
chromashift             mcdeint                 vfrdet
ciescope                mcompand                vibrance
codecview               median                  vibrato
color                   mergeplanes             vidstabdetect
colorbalance            mestimate               vidstabtransform
colorchannelmixer       metadata                vif
colorchart              midequalizer            vignette
colorcontrast           minterpolate            virtualbass
colorcorrect            mix                     vmafmotion
colorhold               monochrome              volume
colorize                morpho                  volumedetect
colorkey                movie                   vpp_qsv
colorlevels             mpdecimate              vstack
colormap                mptestsrc               vstack_qsv
colormatrix             msad                    vstack_vaapi
colorspace              multiply                w3fdif
colorspace_cuda         negate                  waveform
colorspectrum           nlmeans                 weave
colortemperature        nnedi                   xbr
compand                 noformat                xcorrelate
compensationdelay       noise                   xfade
concat                  normalize               xmedian
convolution             null                    xstack
convolve                nullsink                xstack_qsv
copy                    nullsrc                 xstack_vaapi
corr                    oscilloscope            yadif
cover_rect              overlay                 yadif_cuda
crop                    overlay_cuda            yaepblur
cropdetect              overlay_qsv             yuvtestsrc
crossfeed               overlay_vaapi           zmq
crystalizer             owdenoise               zoneplate
cue                     pad                     zoompan
curves                  pad_vaapi               zscale

Enabled bsfs:
aac_adtstoasc           h264_redundant_pps      pgs_frame_merge
av1_frame_merge         hapqa_extract           prores_metadata
av1_frame_split         hevc_metadata           remove_extradata
av1_metadata            hevc_mp4toannexb        setts
chomp                   imx_dump_header         showinfo
dca_core                media100_to_mjpegb      text2movsub
dts2pts                 mjpeg2jpeg              trace_headers
dump_extradata          mjpega_dump_header      truehd_core
dv_error_marker         mov2textsub             vp9_metadata
eac3_core               mpeg2_metadata          vp9_raw_reorder
evc_frame_merge         mpeg4_unpack_bframes    vp9_superframe
extract_extradata       noise                   vp9_superframe_split
filter_units            null                    vvc_metadata
h264_metadata           opus_metadata           vvc_mp4toannexb
h264_mp4toannexb        pcm_rechunk

Enabled indevs:
dshow                   lavfi
gdigrab                 vfwcap

Enabled outdevs:
sdl2

git-essentials external libraries' versions: 

AMF v1.4.32-14-ge1acd43
aom v3.9.0-84-g4073590b26
AviSynthPlus v3.7.3-70-g2b55ba40
ffnvcodec n12.2.72.0-1-g9934f17
freetype VER-2-13-2
fribidi v1.0.14
gsm 1.0.22
harfbuzz 8.4.0-26-gaeadd7c1
lame 3.100
libass 0.17.0-98-ga2c8801
libgme 0.6.3
libopencore-amrnb 0.1.6
libopencore-amrwb 0.1.6
libssh 0.10.6
libtheora 1.1.1
libwebp v1.4.0-2-g3cada4ce
oneVPL 2.10
openmpt libopenmpt-0.6.15-17-gce85dfd37
opus v1.5.2-4-g0dc559f0
rubberband v1.8.1
SDL prerelease-2.29.2-141-g75340b827
speex Speex-1.2.1-20-g3693431
srt v1.5.3-75-gd31d83e
VAAPI 2.22.0.
vidstab v1.1.1-11-gc8caf90
vmaf v3.0.0-77-g450917ce
vo-amrwbenc 0.1.3
vorbis v1.3.7-10-g84c02369
vpx v1.14.0-253-g63b9c2c0e
x264 v0.164.3190
x265 3.6-8-g53afbf5f5
xvid v1.3.7
zeromq 4.3.5
zimg release-3.0.5-150-g7143181

