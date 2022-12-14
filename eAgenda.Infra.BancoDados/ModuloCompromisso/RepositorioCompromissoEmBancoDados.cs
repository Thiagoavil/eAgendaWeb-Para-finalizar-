using eAgenda.Dominio.ModuloCompromisso;
using eAgenda.Dominio.ModuloContato;
using FluentValidation.Results;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace eAgenda.Infra.BancoDados.ModuloCompromisso
{
    public class RepositorioCompromissoEmBancoDados : IRepositorioCompromisso
    {
        private string enderecoBanco;

        public RepositorioCompromissoEmBancoDados()
        {
            enderecoBanco = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json")
                            .Build()
                            .GetConnectionString("SqlServer");
        }

        private const string sqlInserir =
           @"INSERT INTO [TBCOMPROMISSO]
                (
                    [LOCAL],       
                    [DATA], 
                    [ASSUNTO],
                    [HORAINICIO],                    
                    [HORATERMINO],                                                           
                    [CONTATO_NUMERO],
                    [LINK]            
                )
            VALUES
                (
                    @LOCAL,
                    @DATA,
                    @ASSUNTO,
                    @HORAINICIO,
                    @HORATERMINO,
                    @CONTATO_NUMERO,
                    @LINK
                ); SELECT SCOPE_IDENTITY();";

        private const string sqlEditar =
           @" UPDATE [TBCOMPROMISSO]
                    SET 
                        [LOCAL] = @LOCAL, 
                        [DATA] = @DATA, 
                        [ASSUNTO] = @ASSUNTO,
                        [HORAINICIO] = @HORAINICIO, 
                        [HORATERMINO] = @HORATERMINO,
                        [CONTATO_NUMERO] = @CONTATO_NUMERO,
                        [LINK] = @LINK

                    WHERE [NUMERO] = @NUMERO";

        private const string sqlExcluir =
           @"DELETE FROM [TBCOMPROMISSO] 
                WHERE [NUMERO] = @NUMERO";

        private const string sqlSelecionarTodos =
           @"SELECT 
                CP.[NUMERO],       
                CP.[DATA],
                CP.[ASSUNTO],
                CP.[LOCAL],             
                CP.[HORAINICIO],                    
                CP.[HORATERMINO],                                
                CP.[CONTATO_NUMERO],
                CP.[LINK],
                CT.[NOME],       
                CT.[EMAIL],             
                CT.[TELEFONE],                    
                CT.[CARGO], 
                CT.[EMPRESA] 
            FROM
                [TBCOMPROMISSO] AS CP LEFT JOIN 
                [TBCONTATO] AS CT
            ON
                CT.NUMERO = CP.CONTATO_NUMERO";

        private const string sqlSelecionarPorNumero =
           @"SELECT 
                CP.[NUMERO],       
                CP.[DATA],
                CP.[ASSUNTO],
                CP.[LOCAL],             
                CP.[HORAINICIO],                    
                CP.[HORATERMINO],                                
                CP.[CONTATO_NUMERO],
                CP.[LINK],
                CT.[NOME],       
                CT.[EMAIL],             
                CT.[TELEFONE],                    
                CT.[CARGO], 
                CT.[EMPRESA] 
            FROM
                [TBCOMPROMISSO] AS CP LEFT JOIN 
                [TBCONTATO] AS CT
            ON
                CT.NUMERO = CP.CONTATO_NUMERO
            WHERE 
                CP.[NUMERO] = @NUMERO";

        private const string sqlSelecionarCompromissosPassados =
            @"SELECT 
                CP.[NUMERO],       
                CP.[DATA],
                CP.[ASSUNTO],
                CP.[LOCAL],             
                CP.[HORAINICIO],                    
                CP.[HORATERMINO],                                
                CP.[CONTATO_NUMERO],
                CP.[LINK],
                CT.[NOME],       
                CT.[EMAIL],             
                CT.[TELEFONE],                    
                CT.[CARGO], 
                CT.[EMPRESA] 
            FROM
                [TBCOMPROMISSO] AS CP LEFT JOIN 
                [TBCONTATO] AS CT
            ON
                CT.NUMERO = CP.CONTATO_NUMERO
            WHERE 
                CP.[DATA] <= @DATA";

        private const string sqlSelecionarCompromissosFuturos =
           @"SELECT 
                CP.[NUMERO],       
                CP.[DATA],
                CP.[ASSUNTO],
                CP.[LOCAL],             
                CP.[HORAINICIO],                    
                CP.[HORATERMINO],                                
                CP.[CONTATO_NUMERO],
                CP.[LINK],
                CT.[NOME],       
                CT.[EMAIL],             
                CT.[TELEFONE],
                CT.[CARGO], 
                CT.[EMPRESA] 
            FROM
                [TBCOMPROMISSO] AS CP LEFT JOIN 
                [TBCONTATO] AS CT
            ON
                CT.NUMERO = CP.CONTATO_NUMERO
            WHERE 
                CP.[DATA] BETWEEN @DATAINICIAL AND @DATAFINAL
                --CP.[DATA] >= @DATAINICIAL AND CP.[DATA] <= @DATAFINAL";

        private const string sqlVerificarHorarioOcupado =
           @"SELECT
	            COUNT(*)
            FROM 
	            TBCOMPROMISSO
            WHERE 
                [DATA] = @DATA 
            AND                 
                @HORA_INICIO_DESEJADO BETWEEN HORAINICIO AND HORATERMINO 
            OR                 
                @HORA_TERMINO_DESEJADO BETWEEN HORAINICIO AND HORATERMINO";

        public ValidationResult Inserir(Compromisso novoRegistro)
        {
            var validador = new ValidadorCompromisso();

            var resultadoValidacao = validador.Validate(novoRegistro);

            if (resultadoValidacao.IsValid == false)
                return resultadoValidacao;

            int horarioOcupado = VerificarHorarioOcupado(novoRegistro.Data, novoRegistro.HoraInicio, novoRegistro.HoraTermino);

            if (horarioOcupado > 0)
            {
                resultadoValidacao.Errors.Add(new ValidationFailure("", "Nesta data e horário já tem um compromisso agendado"));
                return resultadoValidacao;
            }

            SqlConnection conexaoComBanco = new SqlConnection(enderecoBanco);

            SqlCommand comandoInsercao = new SqlCommand(sqlInserir, conexaoComBanco);

            ConfigurarParametrosCompromisso(novoRegistro, comandoInsercao);

            conexaoComBanco.Open();
            var id = comandoInsercao.ExecuteScalar();
            novoRegistro.Numero = Convert.ToInt32(id);

            conexaoComBanco.Close();

            return resultadoValidacao;
        }
        
        public ValidationResult Editar(Compromisso registro)
        {
            var validador = new ValidadorCompromisso();

            var resultadoValidacao = validador.Validate(registro);

            if (resultadoValidacao.IsValid == false)
                return resultadoValidacao;

            int horarioOcupado = VerificarHorarioOcupado(registro.Data, registro.HoraInicio, registro.HoraTermino);

            var x = SelecionarPorNumero(registro.Numero);

            var teste = registro.HoraInicio >= x.HoraInicio && registro.HoraTermino <= x.HoraTermino;
            
            if (horarioOcupado > 0 && !teste)
            {
                resultadoValidacao.Errors.Add(new ValidationFailure("", "Nesta data e horário já tem um compromisso agendado"));
                return resultadoValidacao;
            }

            SqlConnection conexaoComBanco = new SqlConnection(enderecoBanco);

            SqlCommand comandoEdicao = new SqlCommand(sqlEditar, conexaoComBanco);

            ConfigurarParametrosCompromisso(registro, comandoEdicao);

            conexaoComBanco.Open();
            comandoEdicao.ExecuteNonQuery();
            conexaoComBanco.Close();

            return resultadoValidacao;
        }

        public ValidationResult Excluir(Compromisso registro)
        {
            SqlConnection conexaoComBanco = new SqlConnection(enderecoBanco);

            SqlCommand comandoExclusao = new SqlCommand(sqlExcluir, conexaoComBanco);

            comandoExclusao.Parameters.AddWithValue("NUMERO", registro.Numero);

            conexaoComBanco.Open();
            int numeroRegistrosExcluidos = comandoExclusao.ExecuteNonQuery();

            var resultadoValidacao = new ValidationResult();

            if (numeroRegistrosExcluidos == 0)
                resultadoValidacao.Errors.Add(new ValidationFailure("", "Não foi possível remover o registro"));

            conexaoComBanco.Close();

            return resultadoValidacao;
        }

        public List<Compromisso> SelecionarCompromissosFuturos(DateTime dataInicial, DateTime dataFinal)
        {
            SqlConnection conexaoComBanco = new SqlConnection(enderecoBanco);

            SqlCommand comandoSelecao = new SqlCommand(sqlSelecionarCompromissosFuturos, conexaoComBanco);

            comandoSelecao.Parameters.AddWithValue("DATAINICIAL", dataInicial);
            comandoSelecao.Parameters.AddWithValue("DATAFINAL", dataFinal);

            conexaoComBanco.Open();
            SqlDataReader leitorCompromisso = comandoSelecao.ExecuteReader();

            List<Compromisso> compromissos = new List<Compromisso>();

            while (leitorCompromisso.Read())
            {
                Compromisso compromisso = ConverterParaCompromisso(leitorCompromisso);

                compromissos.Add(compromisso);
            }

            conexaoComBanco.Close();

            return compromissos;
        }

        public List<Compromisso> SelecionarCompromissosPassados(DateTime dataDeHoje)
        {
            SqlConnection conexaoComBanco = new SqlConnection(enderecoBanco);

            SqlCommand comandoSelecao = new SqlCommand(sqlSelecionarCompromissosPassados, conexaoComBanco);

            comandoSelecao.Parameters.AddWithValue("DATA", dataDeHoje);

            conexaoComBanco.Open();
            SqlDataReader leitorCompromisso = comandoSelecao.ExecuteReader();

            List<Compromisso> compromissos = new List<Compromisso>();

            while (leitorCompromisso.Read())
            {
                Compromisso compromisso = ConverterParaCompromisso(leitorCompromisso);

                compromissos.Add(compromisso);
            }

            conexaoComBanco.Close();

            return compromissos;
        }

        public Compromisso SelecionarPorNumero(int numero)
        {
            SqlConnection conexaoComBanco = new SqlConnection(enderecoBanco);

            SqlCommand comandoSelecao = new SqlCommand(sqlSelecionarPorNumero, conexaoComBanco);

            comandoSelecao.Parameters.AddWithValue("NUMERO", numero);

            conexaoComBanco.Open();
            SqlDataReader leitorCompromisso = comandoSelecao.ExecuteReader();

            Compromisso compromisso = null;
            if (leitorCompromisso.Read())
                compromisso = ConverterParaCompromisso(leitorCompromisso);

            conexaoComBanco.Close();

            return compromisso;
        }

        public List<Compromisso> SelecionarTodos()
        {
            SqlConnection conexaoComBanco = new SqlConnection(enderecoBanco);

            SqlCommand comandoSelecao = new SqlCommand(sqlSelecionarTodos, conexaoComBanco);

            conexaoComBanco.Open();
            SqlDataReader leitorCompromisso = comandoSelecao.ExecuteReader();

            List<Compromisso> compromissos = new List<Compromisso>();

            while (leitorCompromisso.Read())
            {
                Compromisso compromisso = ConverterParaCompromisso(leitorCompromisso);

                compromissos.Add(compromisso);
            }

            conexaoComBanco.Close();

            return compromissos;
        }

        private void ConfigurarParametrosCompromisso(Compromisso compromisso, SqlCommand comando)
        {
            comando.Parameters.AddWithValue("NUMERO", compromisso.Numero);
            comando.Parameters.AddWithValue("ASSUNTO", compromisso.Assunto);
            comando.Parameters.AddWithValue("LOCAL", string.IsNullOrEmpty(compromisso.Local) ? DBNull.Value : compromisso.Local);
            comando.Parameters.AddWithValue("LINK", string.IsNullOrEmpty(compromisso.Link) ? DBNull.Value : compromisso.Link);
            comando.Parameters.AddWithValue("DATA", compromisso.Data);
            comando.Parameters.AddWithValue("HORAINICIO", compromisso.HoraInicio.Ticks);
            comando.Parameters.AddWithValue("HORATERMINO", compromisso.HoraTermino.Ticks);

            comando.Parameters.AddWithValue("CONTATO_NUMERO", compromisso.Contato == null ? DBNull.Value : compromisso.Contato.Numero);
        }

        private Compromisso ConverterParaCompromisso(SqlDataReader leitorCompromisso)
        {
            var numero = Convert.ToInt32(leitorCompromisso["NUMERO"]);
            var assunto = Convert.ToString(leitorCompromisso["ASSUNTO"]);

            string local = "", link = "";
            TipoLocalizacaoCompromissoEnum tipoLocalizacao = TipoLocalizacaoCompromissoEnum.Presencial;

            if (leitorCompromisso["LOCAL"] != DBNull.Value)
                local = Convert.ToString(leitorCompromisso["LOCAL"]);

            if (leitorCompromisso["LINK"] != DBNull.Value)
            {
                link = Convert.ToString(leitorCompromisso["LINK"]);
                tipoLocalizacao = TipoLocalizacaoCompromissoEnum.Remoto;
            }

            var data = Convert.ToDateTime(leitorCompromisso["DATA"]);
            var horaInicio = TimeSpan.FromTicks(Convert.ToInt64(leitorCompromisso["HORAINICIO"]));
            var horaTermino = TimeSpan.FromTicks(Convert.ToInt64(leitorCompromisso["HORATERMINO"]));

            Compromisso compromisso = new Compromisso();
            compromisso.Numero = numero;
            compromisso.Assunto = assunto;
            compromisso.Local = local;
            compromisso.Link = link;
            compromisso.Data = data;
            compromisso.HoraInicio = horaInicio;
            compromisso.HoraTermino = horaTermino;
            compromisso.TipoLocal = tipoLocalizacao;

            if (leitorCompromisso["CONTATO_NUMERO"] != DBNull.Value)
            {
                var numeroContato = Convert.ToInt32(leitorCompromisso["CONTATO_NUMERO"]);
                var email = Convert.ToString(leitorCompromisso["EMAIL"]);
                var nome = Convert.ToString(leitorCompromisso["NOME"]);
                var telefone = Convert.ToString(leitorCompromisso["TELEFONE"]);
                var empresa = Convert.ToString(leitorCompromisso["EMPRESA"]);
                var cargo = Convert.ToString(leitorCompromisso["CARGO"]);

                compromisso.Contato = new Contato
                {
                    Numero = numeroContato,
                    Nome = nome,
                    Telefone = telefone,
                    Email = email,
                    Cargo = cargo,
                    Empresa = empresa
                };
            }

            return compromisso;
        }
        
        private int VerificarHorarioOcupado(DateTime data, TimeSpan horaInicioDesejado, TimeSpan horaTerminoDesejado)
        {
            TimeSpan umSegundo = new TimeSpan(0, 0, 1);
            horaInicioDesejado = horaInicioDesejado.Add(umSegundo);
            horaTerminoDesejado = horaTerminoDesejado.Subtract(umSegundo);

            SqlConnection conexaoComBanco = new SqlConnection(enderecoBanco);

            SqlCommand comandoSelecao = new SqlCommand(sqlVerificarHorarioOcupado, conexaoComBanco);

            //stackoverflow.com/questions/8503825/what-is-the-correct-sql-type-to-store-a-net-timespan-with-values-240000
            comandoSelecao.Parameters.AddWithValue("DATA", data);
            comandoSelecao.Parameters.AddWithValue("HORA_INICIO_DESEJADO", horaInicioDesejado.Ticks);
            comandoSelecao.Parameters.AddWithValue("HORA_TERMINO_DESEJADO", horaTerminoDesejado.Ticks);

            conexaoComBanco.Open();
            var quantidadeDeRegistros = Convert.ToInt32(comandoSelecao.ExecuteScalar());            
            conexaoComBanco.Close();

            return quantidadeDeRegistros;
        }
    }
}