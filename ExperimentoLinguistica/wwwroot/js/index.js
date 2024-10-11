document.addEventListener('DOMContentLoaded', function () {

    const btnAceitarTermo = document.getElementById('aceitarTermo');
    if (btnAceitarTermo) {
        document.getElementById('aceitarTermo').addEventListener('change', function () {
            var btnContinuar = document.getElementById('btnContinuar');
            btnContinuar.disabled = !this.checked;
        });
    }

    const btnIniciarTreino = document.getElementById('btnIniciarTreino');
    if (btnIniciarTreino) {
        btnIniciarTreino.addEventListener('click', function () {
            btnIniciarTreino.style.display = 'none';
            document.getElementById('instTreino').style.display = 'none';
            document.getElementById('areaTreino').style.display = 'block';
            const diretorio = "Treino";
            iniciarExperimento(diretorio, idiomaSelecionado, guid);
        });
    }

    const areaExperimento = document.getElementById('areaExperimento');
    if (areaExperimento) {
        areaExperimento.style.display = 'block';
        const diretorio = "Experimento";
        iniciarExperimento(diretorio, idiomaSelecionado, guid);
    }
});


let mediaRecorder;
let audioChunks = [];

window.onload = function () {
    pedirPermissaoMicrofone();
};

function iniciarExperimento(diretorio, idiomaSelecionado, guid) {

    let nomeCampo = 'texto' + diretorio;
    fetch(`/Experimento/ObterTextos?diretorio=${diretorio}&idioma=${idiomaSelecionado}`)
        .then(response => response.json())
        .then(textosExperimento => {
            let currentIndex = 0;
            exibirTexto(textosExperimento[currentIndex], nomeCampo);

            function exibirTexto(linhaTexto, nomeCampo) {
                let [texto, simbolo1, simbolo2,simbolo3 , exemplo, lista] = linhaTexto;

                setTimeout(() => {
                    document.getElementById(nomeCampo).innerText = simbolo1; 
                }, 0);

                setTimeout(() => {
                    document.getElementById(nomeCampo).innerText = simbolo2;
                }, 500);

                setTimeout(() => {
                    document.getElementById(nomeCampo).innerText = exemplo;
                }, 1000);

                setTimeout(() => {
                    document.getElementById(nomeCampo).innerText = simbolo3;
                }, 1050);

                setTimeout(() => {
                    document.getElementById(nomeCampo).innerText = texto;
                    iniciarGravacao(texto, diretorio, idiomaSelecionado, guid, lista);
                }, 1085);

                setTimeout(() => {
                    document.getElementById(nomeCampo).innerText = ""; 
                }, 4085);

                setTimeout(() => {
                    currentIndex++;

                    if (currentIndex < textosExperimento.length) {
                        setTimeout(() => {
                            exibirTexto(textosExperimento[currentIndex], nomeCampo);
                        }, 1000);
                    } else {
                        finalizarExperimento(diretorio, idiomaSelecionado, guid, lista);
                    }
                }, 4085);
            }
        })
        .catch(error => {
            console.error('Erro ao obter os textos: ', error);
        });
}

function pedirPermissaoMicrofone() {
    navigator.mediaDevices.getUserMedia({ audio: true })
        .then(function (stream) {
            console.log("Permissão concedida para usar o microfone.");

        })
        .catch(function (err) {
            console.error("Permissão para usar o microfone foi negada: ", err);
            alert("Para realizar o experimento, é necessário permitir o uso do microfone.");
        });
}

function iniciarGravacao(frase, diretorio, idiomaSelecionado, guid, lista) {
    navigator.mediaDevices.getUserMedia({ audio: true })
        .then(function (stream) {
            mediaRecorder = new MediaRecorder(stream);

            console.log("Gravando áudio do participante...");

            mediaRecorder.start();

            mediaRecorder.addEventListener("dataavailable", function (event) {
                audioChunks.push(event.data);
            });

            setTimeout(function () {
                pararGravacao(frase, diretorio, idiomaSelecionado, guid, lista);
            }, 3000);

        })
        .catch(function (err) {
            console.error("Erro ao acessar o microfone: " + err);
        });
}
function pararGravacao(frase, diretorio, idiomaSelecionado, guid, lista) {
    mediaRecorder.stop();
    console.log("Gravação finalizada.");

    mediaRecorder.addEventListener("stop", function () {
        const audioBlob = new Blob(audioChunks, { type: 'audio/wav' });
        audioChunks = [];
        calcularTempoReacao(audioBlob, function (tempoReacao) {
            console.log("Tempo de reação: ", tempoReacao, " segundos");

            salvarAudio(audioBlob, frase, diretorio, idiomaSelecionado, guid, lista, tempoReacao);
        });
    });
}

function salvarAudio(audioBlob, frase, diretorio, idiomaSelecionado, guid, lista, tempoReacao) {

    const formData = new FormData();
    formData.append('audio', audioBlob, 'gravacao.wav');
    formData.append('frase', frase);
    formData.append("diretorio", diretorio);
    formData.append("idioma", idiomaSelecionado)
    formData.append("guid", guid)
    formData.append("lista", lista)
    formData.append("tempoReacao", tempoReacao)

    fetch('/Experimento/SalvarAudio', {
        method: 'POST',
        body: formData
    }).then(response => {
        if (response.ok) {
            response.json().then(data => {
                console.log('Áudio enviado com sucesso:', data.nomeArquivo);
            });
        } else {
            console.log('Erro ao enviar o áudio.');
        }
    }).catch(error => {
        console.log('Erro: ', error);
    });
}

function calcularTempoReacao(audioBlob, callback) {
    const audioContext = new AudioContext();

    const reader = new FileReader();
    reader.onloadend = function () {
        const arrayBuffer = reader.result;
        audioContext.decodeAudioData(arrayBuffer, function (audioBuffer) {
            const channelData = audioBuffer.getChannelData(0);
            const sampleRate = audioBuffer.sampleRate;

            let reactionTime = 0;
            let limiar = 0.2;
            let foundSound = false;

            for (let i = 0; i < channelData.length; i++) {
                if (Math.abs(channelData[i]) > limiar) {
                    reactionTime = i / sampleRate;
                    foundSound = true;
                    break;
                }
            }

            if (foundSound) {
                callback(reactionTime);
            } else {
                callback(0);
            }
        });
    };
    reader.readAsArrayBuffer(audioBlob);
}

function convertToWav(audioBuffer) {
    return WavEncoder.encode({
        sampleRate: audioBuffer.sampleRate,
        channelData: [audioBuffer.getChannelData(0)]
    });
}

function finalizarExperimento(diretorio, idiomaSelecionado, guid, lista) {
    let nomeCampo = 'texto' + diretorio;

        if (diretorio === 'Experimento') {
            document.getElementById(nomeCampo).innerText = 'Experimento finalizado! Agora, aperte espaço ou clique na tela para fazermos uma pequena avaliação de similiaridade.';
        }
        else {
            document.getElementById(nomeCampo).innerText = 'Treino finalizado! Agora, aperte espaço ou clique na tela para começar o experimento real.';
        }

    document.addEventListener('keydown', function (event) {
        if (event.code === 'Space') {
            if (diretorio === 'Experimento') {
                window.location.href = window.urls.avaliacao;
            } else {
                window.location.href = window.urls.experimento;
            }
        }
    });

    document.addEventListener('click', function () {
        if (diretorio === 'Experimento') {
            window.location.href = window.urls.avaliacao;
        } else {
            window.location.href = window.urls.experimento;
        }
    });
}
