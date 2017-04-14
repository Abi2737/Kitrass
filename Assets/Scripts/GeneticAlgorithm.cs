using System;
using System.Collections;
using System.Collections.Generic;

public class GeneticAlgorithm
{
	class Pair
	{
		public int p1, p2;
	}

	class Chromosome : IComparable                      // structura unui cromozom
	{
		//public TypePieces[] typePieces;				// bucatiile de drum ale cromozomuli
		public bool[] genes;
		public int fitness;								// speranta de viata a cromozomului

		public Chromosome(int numGenes)
		{
			//typePieces = new TypePieces[numPieces];
			genes = new bool[numGenes];
			fitness = 0;
		}

		public int CompareTo(object obj)
		{
			if (obj is Chromosome)
			{
				return this.fitness.CompareTo((obj as Chromosome).fitness);  // compare chromosones fitnesses
			}
			throw new ArgumentException("Object is not a Chromosome");
		}
	}

	Chromosome[] _oldPopulation;               // vechia populatie care evolueaza
	Chromosome[] _newPopulation;               // populatia noua ce evolueaza din cea vechie
	int _dimPop;                               // numarul cromozomilor din populatie

	int _numChromosoms;
	int _numGenesOnChromosome;
	int _maxElitism;
	int _maxCrossover;
	int _maxMutation;
	double _mutationChangePercentage;

	Random _rnd;

	public GeneticAlgorithm(int numChromosoms, int numGenesOnChromosome, double elitismPercentage, double crossoverPercentage,
		double mutationPercentage, double mutationChangePercentage)
	{
		this._numChromosoms = numChromosoms;
		this._numGenesOnChromosome = numGenesOnChromosome;
		this._maxElitism = (int)(numChromosoms * elitismPercentage);
		this._maxCrossover = (int)((numChromosoms - _maxElitism) * crossoverPercentage);
		this._maxMutation = (int)(numChromosoms * mutationPercentage);
		this._mutationChangePercentage = mutationChangePercentage;

		_oldPopulation = new Chromosome[numChromosoms];
		_newPopulation = new Chromosome[numChromosoms];

		_rnd = new Random();

		for (int i = 0; i < numChromosoms; i++)
		{
			_newPopulation[i] = new Chromosome(numGenesOnChromosome);
			_oldPopulation[i] = new Chromosome(numGenesOnChromosome);
		}

		// initializez prima populatie
		// fiecare cromozon are un vector de numPiecesOnChromosome
		// in care se retine tipul unei bucati
		// fitness-ul unui cromozon e nr de bucati care nu sunt SIMPLE_PIECE;
		// primii 2 cromozoni au bucatiile formate numai din SIMPLE_PIECE;
		//for (int i = 0; i < 2; i++)
		//{
		//	for (int j = 0; j < numPiecesOnChromosome; j++)
		//	{
		//		//m_newPopulation[i].typePieces[j] = TypePieces.SIMPLE;
		//		_newPopulation[i].typePieces[j] = false;
		//	}
		//	_newPopulation[i].fitness = 0;
		//}

		// restul cromozoniilor se initializeaza aleator, dar doar cu o
		// schimbare de drum
		int poz;

		for (int i = 2; i < numChromosoms; i++)
		{
			//for (int j = 0; j < numPiecesOnChromosome; j++)
			//{
			//	//m_newPopulation[i].typePieces[j] = TypePieces.SIMPLE;
			//	_newPopulation[i].typePieces[j] = false;
			//}

			// o bucata va fi o schimbare de drum => generez pe ce pozitie va fii
			poz = _rnd.Next(numGenesOnChromosome);
			//m_newPopulation[i].typePieces[poz] = TypePieces.CHANGE;
			_newPopulation[i].genes[poz] = true;
			_newPopulation[i].fitness = 1;
		}

		_dimPop = -1;
	}

	public List<bool> GetGenes()
	{
		var pieces = new List<bool>();
		for (int i = 0; i < _numChromosoms; i++)
		{
			for (int j = 0; j < _numGenesOnChromosome; j++)
			{
				pieces.Add(_newPopulation[i].genes[j]);
			}
		}

		return pieces;
	}

	// creaza o noua populatie 
	public void Evolve()
	{
		// noua populatie devine veche pt urmatoarea
		Chromosome[] aux = _oldPopulation;
		_oldPopulation = _newPopulation;
		_newPopulation = aux;

		_dimPop = 0;                                       // dimensiunea populatiei noi e 0

		// sortez vechia populatie depa fitness
		Array.Sort(_oldPopulation);

		Elitism();                                          // copiez MAX_ELITISM cromozoni (cei mai tari) din populatiaVeche in cea noua

		Pair p;                                             // idicele parintiilor ce trebuiesc compinati

		for (int i = 0; i < _maxCrossover; i++)				// apic crossover de MAX_CROSSOVER ori
		{
			p = SelectParents();                            // aleg parintii ce se vor combina prin crossover
			CrossOver(p);                                   // incrucisarea celor 2 parinti da nastere la 2 comozomi
		}

		Copy(_numChromosoms - _dimPop);                      // copiez resutul pana la colpletarea nr de cromozomi din populatie

		for (int i = 0; i < _maxMutation; i++)        // aplic mutatia de MAX_MUTATION ori
		{
			Mutation();                                     // apic mutatia la nivelul noii populatii
		}
	}

	// copierea celor mai buni cromozomi din populatia vechie in cea noua
	private void Elitism()
	{
		// reprezinta copierea celor mai buni MAX_ELITISM in noua populatie
		for (int i = 0; i < _maxElitism; i++)
		{
			_newPopulation[_dimPop] = _oldPopulation[_numChromosoms - i - 1];
			_dimPop++;
		}
	}

	// selectarea parintiilor pt incrutisare
	private Pair SelectParents()
	{
		Pair result = new Pair();

		result.p1 = _rnd.Next(_numChromosoms);

		while (true)
		{
			result.p2 = _rnd.Next(_numChromosoms);

			if (result.p1 != result.p2)
				break;
		}

		return result;
	}

	private void CalcFitness(Chromosome c)
	{
		for (int i = 0; i < _numGenesOnChromosome; i++)
		{
			if (c.genes[i] != /*TypePieces.SIMPLE*/ true)
			{
				c.fitness++;
			}
		}
	}

	private void ChangeInfo(int p1, int p2)
	{
		int mijl = _numGenesOnChromosome / 2;                    // punctul de incrucisare

		// primul cromozon rezultat
		_newPopulation[_dimPop] = _oldPopulation[p1];
		_newPopulation[_dimPop].fitness = 0;					 // fitness-ul devine 0;

		// informatia copiata din al doilea parinte
		for (int i = mijl; i < _numGenesOnChromosome; i++)
		{
			_newPopulation[_dimPop].genes[i] = _oldPopulation[p2].genes[i];
		}

		CalcFitness(_newPopulation[_dimPop]);
	}

	// incrucisarea parintilor p1, p2, dand nastere la alti 2 copii
	private void CrossOver(Pair p)
	{
		// se foloseste incrucisarea intr-un pct. Se alege un pct de incrucisare
		// la mijlocul secventei de drumuri
		// Din primul parinte este copiata secventa de la inceput pana la punctul
		// de incrucisare, iar din al doilea parinte este copiata secventa de la
		// pct de incrucisare pana la final

		ChangeInfo(p.p1, p.p2);
		_dimPop++;                                                 // creste nr populatiei

		ChangeInfo(p.p2, p.p1);
		_dimPop++;                                                 // creste nr populatiei
	}

	// copierea pana la completarea nr de cromozomi in noua populatie
	private void Copy(int num)
	{
		// dupa ce s-a efectuat incrucisarea si elitismul mai raman "num" pozitii
		// libere in noua populatie
		// aceastea se vor umple aleator cu cromozoni din vechea populatie

		for (int i = 0; i < num; i++)
		{
			_newPopulation[_dimPop] = _oldPopulation[_rnd.Next(_numChromosoms - _maxElitism)];
			_dimPop++;
		}
	}

	// mutatia ce impiedica sa intre algo intr-un optim global
	private void Mutation()
	{
		// aleg un mutant si toate bucatile pe care le gasesc SIMPLE_PIECE e o sansa de
		// percentageOfMutation ca sa le transform in CHANGE_PIECE, si invers

		int mutant = _rnd.Next(_numChromosoms);						// indicele din noua populatie a chomozomului mutant
																	

		_newPopulation[mutant].fitness = 0;							// fitness-ul devine 0; se va modifica

		double r;
		for (int i = 0; i < _numGenesOnChromosome; i++)
		{
			r = _rnd.NextDouble();

			if (_newPopulation[mutant].genes[i] == /*TypePieces.SIMPLE*/ false)
			{
				if (r <= _mutationChangePercentage)						// atunci le voi transforma
				{
					_newPopulation[mutant].genes[i] = true;
					_newPopulation[mutant].fitness++;				// cresc fitness-ul
				}
			}
			else
			{
				if (r <= _mutationChangePercentage)
				{
					_newPopulation[mutant].genes[i] = false;
				}
				else
				{
					_newPopulation[mutant].fitness++;				// cresc fitness-ul
				}
			}
		}
	}
}
