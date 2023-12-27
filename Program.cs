bool IsNullable(IEnumerable<string> rules, string input, IEnumerable<string> terminals)
{
    var enumerableProducts = rules.ToList();
    var enumerableTerminals = terminals.ToList();
    var flag = false;

    if (input == "#")
        return true;

    if (enumerableTerminals.Contains(input))
        return false;

    if (input.Length == 1)
    {
        foreach (var product in enumerableProducts.Where(product => product.StartsWith(input)))
        {
            return IsNullable(enumerableProducts, product[3..], enumerableTerminals);
        }
    }
    foreach (var character in input)
    {
        foreach (var product in enumerableProducts)
        {
            if (product.StartsWith(character) && product.EndsWith("#"))
            {
                flag = true;
                break;
            }
            flag = false;
        }
    }
    return flag;
}
IEnumerable<string> CalculateFirst(string input, IEnumerable<string> rules, IEnumerable<string> variableList,
    IEnumerable<string> terminalList, string startSymbol)
{


    var varEnumerable = variableList.ToList();
    var rulesEnumerable = rules.ToList();
    var terEnumerable = terminalList.ToList();

    var first = new List<string>();

    switch (input.Length)
    {
        case 1 when !varEnumerable.Contains(input):
            first.Add(input);
            return first;
        case 1 when varEnumerable.Contains(input):
            {
                var product = rulesEnumerable.Where(x => x.StartsWith(input));
                foreach (var p in product)
                {
                    var pr = p.Remove(0, 3);
                    first.AddRange(CalculateFirst(pr, rulesEnumerable, varEnumerable, terEnumerable, startSymbol));
                }

                return first;
            }
        case > 1:
            {
                var isNullFlag = false;
                foreach (var c in input)
                {
                    var list = CalculateFirst(c.ToString(), rulesEnumerable, varEnumerable, terEnumerable, startSymbol).ToList();
                    if (list.Contains("#"))
                    {
                        isNullFlag = true;
                        list.Remove("#");
                        first.AddRange(list);
                    }
                    else
                    {
                        isNullFlag = false;
                        first.AddRange(list);
                        break;
                    }
                }

                if (isNullFlag)
                    first.Add("#");
                break;
            }
    }

    return first;
}
IEnumerable<string> CalculateFollow(string input, IEnumerable<string> rules, ICollection<string> variableList, ICollection<string> terminals
    , string startSymbol)
{
    var follow = new List<string>();

    var list = new List<string>();

    if (input == startSymbol)
    {
        list.Add("$");
    }
    var rulesenumerable = rules.ToList();

    var ruleList = rulesenumerable.Where(x => x[3..].Contains(input) && !x.StartsWith(input)).ToList();
    foreach (var rule in ruleList)
    {
        if (rule.EndsWith(input))
        {
            list.AddRange(CalculateFollow(rule[..1], rulesenumerable, variableList, terminals, startSymbol));
        }
        else
        {
            var inputIndex = rule.IndexOfAny(input.ToCharArray(0, 1));
            list.AddRange(CalculateFirst(rule[(inputIndex + 1)..], rulesenumerable, variableList, terminals, startSymbol));
            if (list.Contains("#"))
            {
                list.AddRange(CalculateFollow(rule[..1], rulesenumerable, variableList, terminals, startSymbol));
                list = list.Distinct().ToList();
                list.Remove("#");
            }
        }
    }

    follow.AddRange(list);
    return follow.ToList();
}
string[,] ParseTable(IEnumerable<string> rules, ICollection<string> variableList, ICollection<string> terminals, string startSymbol)
{
    if (!IsLl1(rules, variableList, terminals, startSymbol))
        throw new Exception("This Grammar is not LL1!!!!");

    var enumerable = rules.ToList();
    var f = false;
    var variableCount = variableList.Count;
    var terminalsCount = terminals.Count;

    var tempList = new List<string>();

    foreach (var variable in variableList)
    {
        f = IsNullable(enumerable, variable, terminals);
        if (f)
        {
            terminalsCount++;
            break;
        }
    }

    var parser = new string[variableCount + 1, terminalsCount + 1];

    var productsNumber = new Dictionary<string, int>();
    var terminalsNumber = new Dictionary<string, int>();
    var variablesNumber = new Dictionary<string, int>();
    var j = 0;

    foreach (var variable in variableList)
    {
        j++;
        parser[j, 0] = variable;
        variablesNumber.Add(variable, j);
    }

    j = 0;

    foreach (var terminal in terminals)
    {
        j++;
        parser[0, j] = terminal;
        terminalsNumber.Add(terminal, j);
    }

    if (f)
    {
        parser[0, (j + 1)] = "$";
        terminalsNumber.Add("$", (j + 1));
    }

    foreach (var product in enumerable)
    {
        var i = enumerable.IndexOf(product) + 1;
        productsNumber.Add(product, i);
    }

    foreach (var product in productsNumber.Keys)
    {
        var productIndex = productsNumber.First(x => x.Key == product).Value;
        if (product[3..4].Contains('#'))
            tempList.AddRange(CalculateFollow(product[..1], enumerable, variableList, terminals, startSymbol));
        else
        {
            tempList.AddRange(CalculateFirst(product[3..], enumerable, variableList, terminals, startSymbol));
            if (tempList.Contains("#") && product.EndsWith(product[3..]))
                tempList.AddRange(CalculateFollow(product[..1], enumerable, variableList, terminals, startSymbol));
            else if (tempList.Contains("#"))
            {
                var pString = product.Remove(3, 1);
                tempList.AddRange(CalculateFirst(pString[3..], enumerable, variableList, terminals, startSymbol));
            }
        }

        tempList.Remove("#");
        var variableIndex = variablesNumber.First(x => x.Key == product[..1]).Value;
        foreach (var item in tempList)
        {
            var terminalIndex = terminalsNumber.First(x => x.Key == item).Value;
            parser[variableIndex, terminalIndex] = productIndex.ToString();
        }
        tempList.Clear();
    }

    return parser;
}
static void PrintTable(string[,] parseTable)
{
    Console.ForegroundColor = ConsoleColor.Cyan;

    for (int i = 0; i < parseTable.GetLength(0); i++)
    {
        for (int j = 0; j < parseTable.GetLength(1); j++)
        {
            Console.Write("{0} ", parseTable[i, j] + "\t");
        }
        Console.Write(Environment.NewLine + Environment.NewLine);
    }
    Console.ForegroundColor = ConsoleColor.White;
}

bool IsLl1(IEnumerable<string> rules, IEnumerable<string> variableList, IEnumerable<string> terminalList, string startSymbol)
{
    var rulesEnumerable = rules.ToList();
    var varsEnumerable = variableList.ToList();
    var terminalsEnumerable = terminalList.ToList();
    var firstList = new List<string>();


    if (varsEnumerable.Select(item => rulesEnumerable.Any(x => x.StartsWith(item) && x[3..4] == item)).Any(b => b))
    {
        return false;
    }

    if (varsEnumerable.Select(x => rulesEnumerable.Any(y => y.StartsWith(x) && rulesEnumerable.Any(z => z[3..4] == x && z.StartsWith(y[3..4]))))
        .Any(b => b))
    {
        return false;
    }

    foreach (var variable in varsEnumerable)
    {
        if (IsNullable(rulesEnumerable, variable, terminalsEnumerable))
        {
            var first = CalculateFirst(variable, rulesEnumerable, varsEnumerable, terminalsEnumerable, startSymbol);
            var follow = CalculateFollow(variable, rulesEnumerable, varsEnumerable, terminalsEnumerable, startSymbol);
            if (first.Intersect(follow).ToList().Count <= 0) continue;
            return false;
        }

        var list = rulesEnumerable.Where(x => x.StartsWith(variable)).ToList();
        if (list.Count >= 2)
        {
            foreach (var rule in list)
            {
                firstList.AddRange(CalculateFirst(rule[3..], rules, variableList, terminalList, startSymbol));
            }

            foreach (var t in firstList)
            {
                var tList = firstList.Where(x => x == t).ToList();
                if (tList.Count >= 2)
                    return false;
            }
        }
    }

    return true;
}

int FindIndexR(string[,] table, string member)
{
    for (var i = 0; i < table.GetLength(0); i++)
    {
        if (member == table[i, 0])
            return i;
    }
    return -1;
}

int FindIndexC(string[,] table, string member)
{
    for (var i = 0; i < table.GetLength(1); i++)
    {
        if (member == table[0, i])
            return i;
    }
    return -1;
}

void Parser(string str, string[,] parseTable, string startSymbol, IEnumerable<string> rules, IEnumerable<string> variableList, IEnumerable<string> terminalList)
{
    var len = str.Length;
    str = str + "$";
    var stack = new Stack<string>();
    stack.Push("$");
    stack.Push(startSymbol);
    var enumerable = rules.ToList();
    var j = 0;
    while (len > 0)
    {
        var top = stack.Pop();
        var i = terminalList.Contains(top) ? FindIndexC(parseTable, top) : FindIndexR(parseTable, top);
        j = FindIndexC(parseTable, str[0].ToString());

        var num = parseTable[i, j];
        if (num is null)
            throw new Exception("can't parse your input!!".ToUpper());
        
        var p = enumerable[int.Parse(num)-1];
        for (var k = p.Length-1; k >=3; k--)
        {
            if (p[k].ToString()!="#") 
                stack.Push(p[k].ToString());
        }

        top = stack.Pop();
        if (top == str[0].ToString())
            str = str.Remove(0, 1);
        else
        {
            stack.Push(top);
        }
        len = str.Length - 1;
    }

    if (str == "$")
    {
        Console.WriteLine("yes");
    }
    else
    {
        Console.WriteLine("no");
    }

}
////////////////////////////////////////////////////////////////////////////////////

Console.WriteLine("Write your grammar (A->a,B->b...): ");
Console.WriteLine("-----------------------------------------------");
Console.ForegroundColor = ConsoleColor.Yellow;
var input = Console.ReadLine();

var startSymbol = input![..1];
var grammarProducts = input.Split(",");

var variables = grammarProducts.Select(s => s[..1]).Distinct().ToList();

var terminals = (from s in grammarProducts
                 from t in s[3..]
                 where !variables.Contains(t.ToString())
                 select t.ToString()).Distinct().ToList();

terminals.Remove("#");

Console.ForegroundColor = ConsoleColor.White;
var p = ParseTable(grammarProducts, variables, terminals, startSymbol);
Console.WriteLine("-----------------------------------------------");
Console.WriteLine();
PrintTable(p);
Console.WriteLine("-----------------------------------------------");
for (var i = 1; i <= grammarProducts.Length; i++)
{
    Console.WriteLine($"{i}){grammarProducts[i - 1]}");
}
var str = Console.ReadLine();
Parser(str, p, startSymbol, grammarProducts,variables, terminals);