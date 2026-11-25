import { describe, expect, it } from 'vitest';
import {
  bindingPath,
  bindingRoot,
  extractBindingExtensions,
  extractBindings,
  parseXamlFile,
} from '../../src/graph/xaml/parser';

describe('bindingRoot', () => {
  it('returns the leading identifier of an x:Bind path', () => {
    expect(bindingRoot('ViewModel.StatusMessage')).toBe('ViewModel');
    expect(bindingRoot('  Name')).toBe('Name');
    expect(bindingRoot('SaveCommand')).toBe('SaveCommand');
  });

  it('accepts a leading identifier followed by a call, cast or generic', () => {
    expect(bindingRoot('Format(Name)')).toBe('Format');
    expect(bindingRoot('(sys:String)Value')).toBeNull();
    expect(bindingRoot('Items[0]')).toBeNull();
    expect(bindingRoot('Items, Mode=OneWay')).toBe('Items');
  });

  it('returns null for an empty expression or one that is itself markup', () => {
    expect(bindingRoot('')).toBeNull();
    expect(bindingRoot('   ')).toBeNull();
    expect(bindingRoot('{Binding Name}')).toBeNull();
  });

  it('returns null when the expression does not start with an identifier', () => {
    expect(bindingRoot('1 + 2')).toBeNull();
    expect(bindingRoot('@value')).toBeNull();
  });
});

describe('bindingPath', () => {
  it('splits the segments that follow the root', () => {
    expect(bindingPath('ViewModel.Status.Text', 'ViewModel')).toEqual(['Status', 'Text']);
  });

  it('stops at a call, an indexer, an argument or a space', () => {
    expect(bindingPath('ViewModel.Format(Name)', 'ViewModel')).toEqual(['Format']);
    expect(bindingPath('ViewModel.Items[0]', 'ViewModel')).toEqual(['Items']);
    expect(bindingPath('ViewModel A', 'ViewModel')).toEqual([]);
    expect(bindingPath('ViewModel,Mode=OneWay', 'ViewModel')).toEqual([]);
  });

  it('returns nothing without a root', () => {
    expect(bindingPath('ViewModel.Status', null)).toEqual([]);
  });

  it('ignores repeated dots and a trailing dot', () => {
    expect(bindingPath('ViewModel..Status.', 'ViewModel')).toEqual(['Status']);
  });
});

describe('extractBindingExtensions', () => {
  it('reads an x:Bind expression', () => {
    expect(extractBindingExtensions('{x:Bind ViewModel.Name}')).toEqual([
      { expression: 'ViewModel.Name', dialect: 'x:Bind' },
    ]);
  });

  it('reads the first argument of an x:Bind that carries a mode', () => {
    expect(extractBindingExtensions('{x:Bind ViewModel.Name, Mode=OneWay}')).toEqual([
      { expression: 'ViewModel.Name', dialect: 'x:Bind' },
    ]);
  });

  it('reads {Binding} shorthand and an explicit Path=', () => {
    expect(extractBindingExtensions('{Binding Name}')).toEqual([{ expression: 'Name', dialect: 'Binding' }]);
    expect(extractBindingExtensions('{Binding Path=Name, Mode=OneWay}')).toEqual([
      { expression: 'Name', dialect: 'Binding' },
    ]);
    expect(extractBindingExtensions('{Binding Mode=OneWay, Path=Name}')).toEqual([
      { expression: 'Name', dialect: 'Binding' },
    ]);
  });

  it('reads a TemplateBinding', () => {
    expect(extractBindingExtensions('{TemplateBinding Content}')).toEqual([
      { expression: 'Content', dialect: 'TemplateBinding' },
    ]);
  });

  it('reads several bindings out of one value, in source order', () => {
    expect(extractBindingExtensions('{x:Bind A} and {Binding B}')).toEqual([
      { expression: 'A', dialect: 'x:Bind' },
      { expression: 'B', dialect: 'Binding' },
    ]);
  });

  it('keeps a nested binding intact and stops at the top-level comma', () => {
    expect(extractBindingExtensions('{x:Bind Format({Binding Name}), Mode=OneWay}')[0].expression).toBe('Format({Binding Name})');
  });

  it('tolerates an unclosed binding by taking the rest of the value', () => {
    expect(extractBindingExtensions('{x:Bind Name')).toEqual([{ expression: 'Name', dialect: 'x:Bind' }]);
  });

  it('ignores values that hold no binding', () => {
    expect(extractBindingExtensions('plain')).toEqual([]);
    expect(extractBindingExtensions('{StaticResource Brush}')).toEqual([]);
    expect(extractBindingExtensions('')).toEqual([]);
  });

  it('is case-insensitive about the keyword', () => {
    expect(extractBindingExtensions('{binding Name}')[0].dialect).toBe('Binding');
    expect(extractBindingExtensions('{X:BIND Name}')[0].dialect).toBe('x:Bind');
  });

  it('extractBindings returns the expressions alone', () => {
    expect(extractBindings('{x:Bind A} {Binding B}')).toEqual(['A', 'B']);
  });
});

describe('parseXamlFile', () => {
  const page = `<?xml version="1.0" encoding="utf-8"?>
<Page
    x:Class="Sample.Views.SamplePage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:models="using:Sample.Models"
    xmlns:views="using:Sample.Views">

    <Grid x:Name="Root">
        <TextBlock Text="{x:Bind ViewModel.StatusMessage, Mode=OneWay}" />
        <Button Content="{Binding SaveLabel}" Click="OnSave" />
        <views:SetUpPanel Visibility="{TemplateBinding Visibility}" />
        <Grid.RowDefinitions />
        <ListView ItemsSource="{x:Bind ViewModel.Cells}">
            <ListView.ItemTemplate>
                <DataTemplate x:DataType="models:PermissionCell">
                    <TextBlock Text="{x:Bind Title}" />
                    <TextBlock Text="{x:Bind Missing}" />
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
        <Button Click="Not An Identifier" Tapped="OnTap" />
    </Grid>
</Page>`;

  it('records the root element and the x:Class', () => {
    const file = parseXamlFile('Views/SamplePage.xaml', page);
    expect(file.rootTag).toBe('Page');
    expect(file.className).toBe('Sample.Views.SamplePage');
  });

  it('resolves using: prefixes into CLR namespaces', () => {
    const file = parseXamlFile('Views/SamplePage.xaml', page);
    expect(file.xmlnsUsing).toEqual({
      models: 'Sample.Models',
      views: 'Sample.Views',
    });
  });

  it('collects x:Name values', () => {
    expect(parseXamlFile('a.xaml', page).elementNames).toEqual(['Root']);
  });

  it('collects instantiated element tags by local name and skips property elements', () => {
    const tags = parseXamlFile('a.xaml', page).elementTags;
    expect(tags).toContain('SetUpPanel');
    expect(tags).toContain('DataTemplate');
    expect(tags).not.toContain('Grid.RowDefinitions');
    expect(tags).not.toContain('views:SetUpPanel');
  });

  it('records event handlers that are identifiers, and nothing else', () => {
    const handlers = parseXamlFile('a.xaml', page).handlerNames;
    expect(handlers).toEqual(['OnSave', 'OnTap']);
  });

  it('reads page-level bindings with their dialect, expression and line', () => {
    const file = parseXamlFile('a.xaml', page);
    const status = file.bindings.find((binding) => binding.expression === 'ViewModel.StatusMessage');
    expect(status).toMatchObject({
      dialect: 'x:Bind',
      root: 'ViewModel',
      path: ['StatusMessage'],
      line: 10,
      inTemplate: false,
      templateDataType: null,
      templateTypeNamespace: null,
    });

    const classic = file.bindings.find((binding) => binding.expression === 'SaveLabel');
    expect(classic).toMatchObject({ dialect: 'Binding', root: null, path: ['SaveLabel'] });

    const templateBinding = file.bindings.find((binding) => binding.dialect === 'TemplateBinding');
    expect(templateBinding?.root).toBeNull();
  });

  it('scopes a binding inside a DataTemplate to that template’s x:DataType', () => {
    const file = parseXamlFile('a.xaml', page);
    const inTemplate = file.bindings.filter((binding) => binding.inTemplate);
    expect(inTemplate.map((binding) => binding.root)).toEqual(['Title', 'Missing']);
    expect(inTemplate[0].templateDataType).toBe('models:PermissionCell');
    expect(inTemplate[0].templateTypeNamespace).toBe('Sample.Models.PermissionCell');
  });

  it('records the x:DataType declarations with the line of the attribute, not of the element', () => {
    const file = parseXamlFile('a.xaml', page);
    expect(file.dataTypes).toEqual([{ value: 'models:PermissionCell', line: 16 }]);
  });

  it('locates a binding on the attribute line of a multi-line element', () => {
    const file = parseXamlFile('multi.xaml', `<Page x:Class="N.P"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <TextBlock
      Text="static"
      Tag="{x:Bind ViewModel.Tag}" />
</Page>`);
    expect(file.bindings[0].line).toBe(5);
  });

  it('handles a file with no x:Class without inventing one', () => {
    const file = parseXamlFile('Styles.xaml', '<ResourceDictionary><Style x:Key="A" /></ResourceDictionary>');
    expect(file.className).toBeNull();
    expect(file.rootTag).toBe('ResourceDictionary');
  });

  it('keeps the first x:Class when a file repeats one', () => {
    const file = parseXamlFile('a.xaml', '<Page x:Class="First"><Grid x:Class="Second" /></Page>');
    expect(file.className).toBe('First');
  });

  it('resolves a template type whose prefix is not declared to its local name', () => {
    const file = parseXamlFile('a.xaml', `<Page x:Class="N.P">
  <DataTemplate x:DataType="models:Row"><TextBlock Text="{x:Bind Name}" /></DataTemplate>
</Page>`);
    const binding = file.bindings[0];
    expect(binding.templateDataType).toBe('models:Row');
    expect(binding.templateTypeNamespace).toBe('Row');
  });

  it('treats a template without x:DataType as unbounded', () => {
    const file = parseXamlFile('a.xaml', `<Page x:Class="N.P">
  <DataTemplate><TextBlock Text="{x:Bind Name}" /></DataTemplate>
</Page>`);
    expect(file.bindings[0]).toMatchObject({ inTemplate: true, templateDataType: null, templateTypeNamespace: null });
  });

  it('leaves a template scope when its element closes', () => {
    const file = parseXamlFile('a.xaml', `<Page x:Class="N.P">
  <DataTemplate x:DataType="Row"><TextBlock Text="{x:Bind Inner}" /></DataTemplate>
  <TextBlock Text="{x:Bind Outer}" />
</Page>`);
    expect(file.bindings.map((binding) => `${binding.root}:${binding.inTemplate}`)).toEqual([
      'Inner:true', 'Outer:false',
    ]);
  });

  it('records an empty file without throwing', () => {
    const file = parseXamlFile('empty.xaml', '');
    expect(file.rootTag).toBe('');
    expect(file.bindings).toEqual([]);
  });
});

describe('parseXamlFile: several bindings in one attribute', () => {
  it('locates each binding on the attribute line it shares', () => {
    const file = parseXamlFile('multi.xaml', `<Page x:Class="N.P">
  <TextBlock Text="{x:Bind First} {x:Bind Second}" />
</Page>`);
    expect(file.bindings.map((binding) => binding.line)).toEqual([2, 2]);
  });
});

describe('parseXamlFile: a blank x:DataType', () => {
  it('treats whitespace as no row type', () => {
    const file = parseXamlFile('a.xaml', `<Page x:Class="N.P">
  <DataTemplate x:DataType="   "><TextBlock Text="{x:Bind Row}" /></DataTemplate>
</Page>`);
    expect(file.bindings[0]).toMatchObject({ inTemplate: true, templateDataType: null });
  });
});
